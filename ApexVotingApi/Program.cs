using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient(connectionName: "cache");
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


List<string> games = ["Apex Legends", "Fortnite", "Call of Duty", "Valorant", "Overwatch"];

app.UseHttpsRedirection();
app.MapGet("/", () => "Welcome to the Apex Voting API!");
app.MapPost("/votes", (IConnectionMultiplexer redis, [FromBody] string vote) =>
{
    var db = redis.GetDatabase();
    
    if (!games.Contains(vote))
    {
        return Results.BadRequest("Invalid game choice.");
    }
    
    db.StringIncrement(vote);
    return Results.Ok($"Vote for {vote} recorded.");
});

app.MapGet("/leaderboard", (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var leaderboard = new Dictionary<string, int>();
    
    foreach (var game in games)
    {
        var votes = db.StringGet(game);
        leaderboard[game] = votes.IsNull? 0 : (int)votes;
    }
    
    var sortedLeaderboard = leaderboard.OrderByDescending(kv => kv.Value)
                                       .ToDictionary(kv => kv.Key, kv => kv.Value);
    
    return Results.Ok(sortedLeaderboard);
});

app.Run();