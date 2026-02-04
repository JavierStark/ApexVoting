using System.Diagnostics;
using System.Text.Json;
using ApexVoting.ServiceDefaults;
using StackExchange.Redis;

namespace ApexVotingProcessor;

public class Worker(ILogger<Worker> logger, IConnectionMultiplexer redis, IServiceScopeFactory scopeFactory) : BackgroundService
{
    
    private const int BATCH_SIZE = 100;
    private const int MAX_BATCH_WAIT_MS = 5000;
    private const int EMPTY_QUEUE_DELAY_MS = 100;
    private const int QUEUE_LAG_REPORT_INTERVAL_MS = 5000;
    private long _currentQueueLag;

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cache = redis.GetDatabase();
        var votes = new List<Vote>();
        var batchTimer = Stopwatch.StartNew();
        var queueLagTimer = Stopwatch.StartNew();
        
        // Register the observable gauge with a callback to read _currentQueueLag
        var queueLagGauge = VotingMetrics.CreateQueueLagGauge(() => _currentQueueLag);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Update queue lag metric every 5 seconds
                if (queueLagTimer.ElapsedMilliseconds > QUEUE_LAG_REPORT_INTERVAL_MS)
                {
                    await UpdateQueueLagAsync(cache);
                    queueLagTimer.Restart();
                }
                
                var batchItems = await PopBatchAsync(cache, BATCH_SIZE, stoppingToken);
                
                if (batchItems.Count == 0)
                {
                    // Flush partial batch if waiting too long
                    if (votes.Count > 0 && batchTimer.ElapsedMilliseconds > MAX_BATCH_WAIT_MS)
                    {
                        await FlushVotesAsync(votes, stoppingToken);
                        batchTimer.Restart();
                    }
                    
                    await Task.Delay(EMPTY_QUEUE_DELAY_MS, stoppingToken);
                    continue;
                }
                
                // Deserialize and add to buffer
                foreach (var item in batchItems)
                {
                    try
                    {
                        var voteDto = JsonSerializer.Deserialize<VoteDto>((byte[])item!);
                        if (voteDto != null)
                        {
                            votes.Add(new Vote
                            {
                                VoterId = voteDto.Id,
                                Candidate = voteDto.Game
                            });
                        }
                    }
                    catch (JsonException ex)
                    {
                        logger.LogWarning(ex, "Failed to deserialize vote, skipping");
                    }
                }
                
                // Flush when batch is full
                if (votes.Count < BATCH_SIZE) continue;
                
                await FlushVotesAsync(votes, stoppingToken);
                batchTimer.Restart();
            }
        }
        finally
        {
            // Flush remaining votes on shutdown
            if (votes.Count > 0)
            {
                try
                {
                    await FlushVotesAsync(votes, CancellationToken.None);
                    logger.LogInformation("Flushed {Count} remaining votes on shutdown", votes.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to flush votes on shutdown");
                }
            }
        }
    }
    
    private async Task UpdateQueueLagAsync(IDatabase cache)
    {
        try
        {
            var queueLength = await cache.ListLengthAsync("votes");
            _currentQueueLag = queueLength;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get queue length for metrics");
        }
    }
    
    private static async Task<List<RedisValue>> PopBatchAsync(
        IDatabase cache, 
        int count, 
        CancellationToken cancellationToken)
    {
        var results = new List<RedisValue>(count);
        
        for (var i = 0; i < count && !cancellationToken.IsCancellationRequested; i++)
        {
            var value = await cache.ListRightPopAsync("votes");
            if (value.IsNull)
                break;
                
            results.Add(value);
        }
        
        return results;
    }
    
    private async Task FlushVotesAsync(List<Vote> votes, CancellationToken cancellationToken)
    {
        if (votes.Count == 0) return;
        
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApexVotingDbContext>();
        
        var sw = Stopwatch.StartNew();
        
        await db.Votes.AddRangeAsync(votes, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        
        VotingMetrics.VotesProcessed.Add(votes.Count);
        
        logger.LogInformation(
            "Flushed {VotesCount} votes to database in {SwElapsedMilliseconds}ms",
            votes.Count, 
            sw.ElapsedMilliseconds);
        
        votes.Clear();
    }
    
    public record VoteDto(string Game, string Id);
}