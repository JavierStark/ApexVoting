using System.Diagnostics.Metrics;

namespace ApexVoting.ServiceDefaults;

public static class VotingMetrics
{
    public static readonly string MeterName = "ApexVoting";
    public static readonly Meter Meter = new(MeterName);
    
    // API Metrics
    public static readonly Counter<long> VotesIngested = Meter.CreateCounter<long>(
        "votes_ingested_total",
        description: "Total number of votes ingested by the API");
    
    // Worker Metrics  
    public static readonly Counter<long> VotesProcessed = Meter.CreateCounter<long>(
        "votes_processed_total", 
        description: "Total number of votes processed and stored to database");
    
    // Queue Lag - needs to be registered with a callback in the Worker
    public static ObservableGauge<long> CreateQueueLagGauge(Func<long> observeValue)
    {
        return Meter.CreateObservableGauge(
            "vote_queue_lag",
            observeValue,
            description: "Current number of votes pending in Redis queue");
    }
}