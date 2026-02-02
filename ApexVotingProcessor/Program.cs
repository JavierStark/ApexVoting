using ApexVotingProcessor;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient(connectionName: "cache");
builder.AddNpgsqlDbContext<ApexVotingDbContext>(connectionName: "postgres");
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

public class ApexVotingDbContext : DbContext
{
    public ApexVotingDbContext(DbContextOptions<ApexVotingDbContext> options) : base(options)
    {
    }

    public DbSet<Votes> Votes => Set<Votes>();
}

public record Votes
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string VoterId { get; set; } = string.Empty;
    public string Candidate { get; set; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

