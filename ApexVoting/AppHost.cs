var builder = DistributedApplication.CreateBuilder(args);


var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("apexvotingdb");

var worker = builder.AddProject<Projects.ApexVotingProcessor>("apexvotingprocessor")
    .WaitFor(cache)
    .WithReference(cache)
    .WaitFor(db)
    .WithReference(db);

var api = builder.AddProject<Projects.ApexVotingApi>("apexvotingapi")
    .WaitFor(cache)
    .WithReference(cache);

var app = builder.Build();


app.Run();