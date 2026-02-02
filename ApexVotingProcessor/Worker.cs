using System.Diagnostics;
using System.Text.Json;
using StackExchange.Redis;

namespace ApexVotingProcessor;

public class Worker(ILogger<Worker> logger, IConnectionMultiplexer redis, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var votes = new List<Vote>();
        var cache = redis.GetDatabase();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApexVotingDbContext>();

            var vote = cache.ListRightPop("votes");

            if (!vote.IsNull)
            {
                var voteObject = JsonSerializer.Deserialize<VoteDto>((byte[])vote!);
                votes.Add(new Vote
                    {
                        VoterId = voteObject.Id,
                        Candidate = voteObject.Game
                    }
                );

                if (votes.Count > 10)
                {
                    await db.Votes.AddRangeAsync(votes, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);
                    votes.Clear();
                    
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Processed 100 votes.");
                    }
                }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
    
    public record VoteDto(string Game, string Id);
}