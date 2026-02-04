using ApexVotingProcessor;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient(connectionName: "cache");
builder.AddNpgsqlDbContext<ApexVotingDbContext>(connectionName: "apexvotingdb");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.MapDefaultEndpoints();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApexVotingDbContext>();
    db.Database.Migrate();
}

host.Run();

public class ApexVotingDbContext : DbContext
{
    public ApexVotingDbContext(DbContextOptions<ApexVotingDbContext> options) : base(options)
    {
    }

    public DbSet<Vote> Votes => Set<Vote>();
}

public record Vote
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string VoterId { get; set; } = string.Empty;
    public string Candidate { get; set; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

