var builder = DistributedApplication.CreateBuilder(args);


var cache = builder.AddRedis("cache");

var worker = builder.AddProject<Projects.ApexVotingProcessor>("apexvotingprocessor")
    .WaitFor(cache)
    .WithReference(cache);

var api = builder.AddProject<Projects.ApexVotingApi>("apexvotingapi")
    .WaitFor(cache)
    .WithReference(cache);

var app = builder.Build();


app.Run();