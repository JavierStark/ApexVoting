# ApexVoting Deployment Guide

## Overview
This guide covers deploying the ApexVoting distributed system using Docker and Kubernetes.

## Architecture
- **ApexVotingApi**: REST API for vote ingestion
- **ApexVotingProcessor**: Background worker for processing votes from Redis to PostgreSQL
- **Redis**: Message queue for votes
- **PostgreSQL**: Persistent storage for votes
- **OpenTelemetry Collector**: Observability and metrics collection

## Prerequisites

### For Docker Compose
- Docker Desktop or Docker Engine 20.10+
- Docker Compose v2.0+

### For Kubernetes
- Kubernetes cluster (minikube, kind, AKS, EKS, GKE, etc.)
- kubectl configured to access your cluster
- Docker for building images

## Local Development with Docker Compose

### 1. Build and Run
```powershell
# Build images
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (clean database)
docker-compose down -v
```

### 2. Access the Services
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/openapi/v1.json
- **Prometheus Metrics**: http://localhost:8889/metrics
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

### 3. Test the API
```powershell
# Submit a vote
curl -X POST http://localhost:5000/votes `
  -H "Content-Type: application/json" `
  -d '{"game":"Apex Legends","id":"user123"}'

# Get leaderboard
curl http://localhost:5000/leaderboard
```

## Production Deployment with Kubernetes

### 1. Build Docker Images

#### Build locally
```powershell
# Build API image
docker build -t apexvotingapi:latest -f ApexVotingApi/Dockerfile .

# Build Processor image
docker build -t apexvotingprocessor:latest -f ApexVotingProcessor/Dockerfile .
```

#### Tag for your registry
```powershell
# For Azure Container Registry
docker tag apexvotingapi:latest yourregistry.azurecr.io/apexvotingapi:latest
docker tag apexvotingprocessor:latest yourregistry.azurecr.io/apexvotingprocessor:latest

# Push to registry
docker push yourregistry.azurecr.io/apexvotingapi:latest
docker push yourregistry.azurecr.io/apexvotingprocessor:latest
```

### 2. Update Deployment Configuration

Edit `deployment.yml` and update:
1. **Image references** (lines with `image:`):
   ```yaml
   image: yourregistry.azurecr.io/apexvotingapi:latest
   image: yourregistry.azurecr.io/apexvotingprocessor:latest
   ```

2. **Secrets** (IMPORTANT - change default passwords):
   ```yaml
   # PostgreSQL password
   # Database connection string
   ```

3. **Resource limits** based on your cluster capacity

4. **Storage class** for PersistentVolumeClaim if needed

### 3. Deploy to Kubernetes

```powershell
# Create namespace and deploy all resources
kubectl apply -f deployment.yml

# Check deployment status
kubectl get all -n apexvoting

# Watch pods starting
kubectl get pods -n apexvoting -w

# Check logs
kubectl logs -n apexvoting -l app=apexvotingapi
kubectl logs -n apexvoting -l app=apexvotingprocessor
```

### 4. Access the Application

#### Using LoadBalancer (cloud providers)
```powershell
# Get external IP
kubectl get service apexvotingapi -n apexvoting

# Wait for EXTERNAL-IP to be assigned
# Access at http://<EXTERNAL-IP>
```

#### Using Port Forward (local/development)
```powershell
# Forward API port
kubectl port-forward -n apexvoting service/apexvotingapi 8080:80

# Access at http://localhost:8080
```

### 5. Monitor the Application

#### View Metrics
```powershell
# Port forward to OTEL collector
kubectl port-forward -n apexvoting service/otel-collector 8889:8889

# Access Prometheus metrics at http://localhost:8889/metrics
```

#### Check Custom Metrics
Look for these custom metrics:
- `votes_ingested_total` - Total votes received by API
- `votes_processed_total` - Total votes written to database
- `vote_queue_lag` - Current Redis queue length

#### View Logs
```powershell
# API logs
kubectl logs -n apexvoting -l app=apexvotingapi -f

# Processor logs
kubectl logs -n apexvoting -l app=apexvotingprocessor -f

# All logs
kubectl logs -n apexvoting --all-containers=true -f
```

## Scaling

### Manual Scaling
```powershell
# Scale API
kubectl scale deployment apexvotingapi -n apexvoting --replicas=5

# Scale Processor
kubectl scale deployment apexvotingprocessor -n apexvoting --replicas=3
```

### Auto-scaling
The deployment includes HorizontalPodAutoscalers (HPA) that automatically scale based on CPU/Memory:
- **API**: 2-10 replicas
- **Processor**: 1-5 replicas

```powershell
# Check HPA status
kubectl get hpa -n apexvoting

# View HPA details
kubectl describe hpa apexvotingapi-hpa -n apexvoting
```

## Troubleshooting

### Pods Not Starting
```powershell
# Describe pod to see events
kubectl describe pod <pod-name> -n apexvoting

# Check if images are accessible
kubectl get events -n apexvoting --sort-by='.lastTimestamp'
```

### Database Connection Issues
```powershell
# Verify PostgreSQL is running
kubectl get pods -n apexvoting -l app=postgres

# Check database logs
kubectl logs -n apexvoting -l app=postgres

# Test connection from processor pod
kubectl exec -it -n apexvoting <processor-pod> -- /bin/bash
# Then: apt-get update && apt-get install -y postgresql-client
# psql -h postgres -U postgres -d apexvotingdb
```

### Redis Connection Issues
```powershell
# Verify Redis is running
kubectl get pods -n apexvoting -l app=redis

# Test Redis connection
kubectl exec -it -n apexvoting <redis-pod> -- redis-cli ping
```

### Check Metrics
```powershell
# View OTEL collector logs
kubectl logs -n apexvoting -l app=otel-collector
```

## Cleanup

### Docker Compose
```powershell
# Stop and remove containers
docker-compose down

# Remove volumes (deletes data)
docker-compose down -v
```

### Kubernetes
```powershell
# Delete all resources in namespace
kubectl delete namespace apexvoting

# Or delete specific deployment
kubectl delete -f deployment.yml
```

## Configuration Reference

### Environment Variables

#### ApexVotingApi
- `ASPNETCORE_ENVIRONMENT`: Environment (Development/Production)
- `ASPNETCORE_HTTP_PORTS`: HTTP port (default: 8080)
- `ConnectionStrings__cache`: Redis connection string
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OpenTelemetry endpoint
- `OTEL_SERVICE_NAME`: Service name for telemetry

#### ApexVotingProcessor
- `DOTNET_ENVIRONMENT`: Environment (Development/Production)
- `ConnectionStrings__cache`: Redis connection string
- `ConnectionStrings__apexvotingdb`: PostgreSQL connection string
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OpenTelemetry endpoint
- `OTEL_SERVICE_NAME`: Service name for telemetry

## Performance Tuning

### Processor Batch Settings
Edit `Worker.cs` constants:
- `BATCH_SIZE`: Number of votes per database write (default: 100)
- `MAX_BATCH_WAIT_MS`: Max wait before flushing partial batch (default: 5000ms)
- `QUEUE_LAG_REPORT_INTERVAL_MS`: Frequency of queue lag metric updates (default: 5000ms)

### Resource Limits
Edit `deployment.yml` to adjust:
- CPU requests/limits
- Memory requests/limits
- Replica counts
- HPA thresholds

## Security Considerations

### Production Checklist
- [ ] Change default PostgreSQL password
- [ ] Use Kubernetes secrets (not ConfigMaps) for sensitive data
- [ ] Enable TLS/SSL for external API access
- [ ] Use private container registry
- [ ] Implement network policies
- [ ] Enable RBAC for service accounts
- [ ] Use pod security policies
- [ ] Configure resource quotas per namespace
- [ ] Enable audit logging
- [ ] Implement rate limiting on API

### Recommended Secrets Management
- Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
- Use external secrets operator for Kubernetes
- Rotate credentials regularly

## Monitoring and Observability

### Integrating with Observability Platforms

#### Prometheus/Grafana
Update `otel-collector-config.yaml` to export to Prometheus

#### Application Insights (Azure)
Add to your .csproj:
```xml
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.0.0" />
```

Configure in Extensions.cs (already has commented example)

#### Jaeger/Zipkin
Update OTEL collector config to export traces

## Common Issues and Solutions

### Issue: Processor can't connect to database
**Solution**: Check that migrations ran successfully. If not:
```powershell
kubectl logs -n apexvoting -l app=apexvotingprocessor | grep -i migration
```

### Issue: High queue lag
**Solution**: Scale processor:
```powershell
kubectl scale deployment apexvotingprocessor -n apexvoting --replicas=3
```

### Issue: Metrics not appearing
**Solution**: Check OTEL collector is running and endpoints are correct:
```powershell
kubectl logs -n apexvoting -l app=otel-collector
```

## Support
For issues, refer to the project documentation or create an issue in the repository.
