# ApexVoting

A distributed voting application built with .NET Aspire to learn modern cloud-native technologies.

## What I Learned

### Redis
- Used as a message queue for asynchronous vote processing
- Implemented pub/sub patterns for real-time communication between services
- Configured Redis for container orchestration and cloud deployments

### Docker
- Created multi-stage Dockerfiles for optimized container images
- Containerized microservices (API and Processor)
- Used docker-compose for local development and service orchestration
- Managed container networking and environment variables

### Kubernetes
- Deployed containerized applications to Kubernetes clusters
- Created deployment manifests and service definitions
- Configured namespaces, pods, and replica sets
- Managed application scaling and health checks
- Used kubectl for cluster management and troubleshooting

### PostgreSQL
- Implemented persistent data storage for voting records
- Configured database connections in containerized environments
- Managed database migrations and schema updates
- Handled connection pooling and resilience patterns

### k6
- Wrote load testing scripts to simulate concurrent users
- Created smoke tests for basic functionality validation
- Analyzed performance metrics and identified bottlenecks
- Tested system behavior under various load conditions

### .NET Aspire
- Built distributed applications with service discovery
- Configured observability with OpenTelemetry
- Implemented service defaults and health checks
- Orchestrated multiple services with a single AppHost

## Architecture

- **ApexVoting**: Aspire AppHost orchestrating the distributed system
- **ApexVotingApi**: REST API for receiving votes
- **ApexVotingProcessor**: Background worker processing votes from the queue
- **ApexVoting.ServiceDefaults**: Shared configuration and telemetry

## Running the Project

```bash
# Local development with Aspire
dotnet run --project ApexVoting

# Docker Compose
docker-compose up

# Kubernetes
kubectl apply -f deployment.yml
```
