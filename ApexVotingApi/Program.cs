using System.Text.Json;
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
app.MapPost("/votes", (IConnectionMultiplexer redis, [FromBody] VoteDto voteDto) =>
{
    var db = redis.GetDatabase();
    
    if (!games.Contains(voteDto.Game))
    {
        return Results.BadRequest("Invalid game choice.");
    }
    
    var payload = JsonSerializer.SerializeToUtf8Bytes(voteDto);
    
    db.ListRightPushAsync("votes", payload, When.Always, CommandFlags.FireAndForget);

    return Results.Ok($"Vote for {voteDto.Game} recorded.");
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

public record VoteDto(string Game, string Id);