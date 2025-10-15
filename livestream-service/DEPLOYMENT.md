# Deployment Guide

This guide covers various deployment options for the Livestream Service.

## Table of Contents
1. [Local Development](#local-development)
2. [Production JAR Deployment](#production-jar-deployment)
3. [Windows Server Deployment](#windows-server-deployment)
4. [Azure Deployment](#azure-deployment)
5. [Docker Deployment](#docker-deployment)
6. [Environment Configuration](#environment-configuration)

## Local Development

### Quick Start
```bash
cd livestream-service
mvn spring-boot:run
```

### With Custom Port
```bash
mvn spring-boot:run -Dspring-boot.run.arguments=--server.port=8081
```

### With Profile
```bash
mvn spring-boot:run -Dspring-boot.run.profiles=dev
```

## Production JAR Deployment

### Build the JAR
```bash
mvn clean package -DskipTests
```

This creates: `target/livestreamservice-0.0.1-SNAPSHOT.jar` (~58MB)

### Run the JAR
```bash
java -jar target/livestreamservice-0.0.1-SNAPSHOT.jar
```

### Run with External Configuration
```bash
java -jar target/livestreamservice-0.0.1-SNAPSHOT.jar \
  --spring.config.location=file:/path/to/application.properties
```

### Run as Background Service (Linux)
```bash
nohup java -jar target/livestreamservice-0.0.1-SNAPSHOT.jar > app.log 2>&1 &
```

### Run with JVM Options
```bash
java -Xms512m -Xmx1024m -jar target/livestreamservice-0.0.1-SNAPSHOT.jar
```

## Windows Server Deployment

### Option 1: Windows Service with NSSM

1. Download NSSM: https://nssm.cc/download

2. Install as service:
```cmd
nssm install LivestreamService "C:\Program Files\Java\jdk-17\bin\java.exe"
nssm set LivestreamService AppParameters "-jar C:\livestream-service\livestreamservice.jar"
nssm set LivestreamService AppDirectory "C:\livestream-service"
nssm set LivestreamService AppStdout "C:\livestream-service\logs\service.log"
nssm set LivestreamService AppStderr "C:\livestream-service\logs\error.log"
nssm start LivestreamService
```

3. Manage the service:
```cmd
nssm start LivestreamService
nssm stop LivestreamService
nssm restart LivestreamService
nssm remove LivestreamService
```

### Option 2: Task Scheduler

1. Create a batch file `start-livestream.bat`:
```batch
@echo off
cd C:\livestream-service
java -jar livestreamservice-0.0.1-SNAPSHOT.jar
```

2. Create a scheduled task to run at system startup

### Option 3: IIS with HttpPlatformHandler

1. Install HttpPlatformHandler for IIS
2. Configure web.config:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="httpPlatformHandler" path="*" verb="*" 
           modules="httpPlatformHandler" resourceType="Unspecified"/>
    </handlers>
    <httpPlatform processPath="C:\Program Files\Java\jdk-17\bin\java.exe"
                  arguments="-jar C:\livestream-service\livestreamservice.jar"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout">
    </httpPlatform>
  </system.webServer>
</configuration>
```

## Azure Deployment

### Option 1: Azure App Service

1. **Create App Service**:
```bash
az webapp create \
  --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --name livestream-service \
  --runtime "JAVA:17-java17"
```

2. **Deploy JAR**:
```bash
az webapp deploy \
  --resource-group myResourceGroup \
  --name livestream-service \
  --src-path target/livestreamservice-0.0.1-SNAPSHOT.jar
```

3. **Configure App Settings**:
```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name livestream-service \
  --settings \
    SPRING_DATASOURCE_URL="jdbc:sqlserver://myserver.database.windows.net:1433;database=DBDACS_Livestream" \
    SPRING_DATASOURCE_USERNAME="adminuser" \
    SPRING_DATASOURCE_PASSWORD="SecurePassword123!" \
    JWT_SECRET="your-secret-key"
```

### Option 2: Azure Container Instances

1. **Build Docker image** (see Docker section below)

2. **Deploy to Azure**:
```bash
az container create \
  --resource-group myResourceGroup \
  --name livestream-service \
  --image livestreamservice:latest \
  --ports 8080 \
  --environment-variables \
    SPRING_DATASOURCE_URL="jdbc:sqlserver://..." \
    SPRING_DATASOURCE_USERNAME="adminuser" \
    SPRING_DATASOURCE_PASSWORD="SecurePassword123!"
```

### Option 3: Azure Kubernetes Service (AKS)

1. Create Kubernetes manifests (see Docker section)
2. Deploy to AKS cluster

## Docker Deployment

### Create Dockerfile

Create `Dockerfile` in the project root:

```dockerfile
FROM eclipse-temurin:17-jre-alpine
WORKDIR /app
COPY target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
```

### Build Docker Image
```bash
mvn clean package -DskipTests
docker build -t livestreamservice:latest .
```

### Run Docker Container
```bash
docker run -d \
  --name livestream-service \
  -p 8080:8080 \
  -e SPRING_DATASOURCE_URL="jdbc:sqlserver://host.docker.internal:1433;databaseName=DBDACS_Livestream" \
  -e SPRING_DATASOURCE_USERNAME="sa" \
  -e SPRING_DATASOURCE_PASSWORD="YourPassword123" \
  -e JWT_SECRET="your-secret-key" \
  livestreamservice:latest
```

### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'
services:
  livestream-service:
    build: .
    ports:
      - "8080:8080"
    environment:
      - SPRING_DATASOURCE_URL=jdbc:sqlserver://sqlserver:1433;databaseName=DBDACS_Livestream
      - SPRING_DATASOURCE_USERNAME=sa
      - SPRING_DATASOURCE_PASSWORD=YourPassword123
      - JWT_SECRET=your-secret-key
    depends_on:
      - sqlserver
    restart: unless-stopped

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    restart: unless-stopped

volumes:
  sqldata:
```

Run with:
```bash
docker-compose up -d
```

### Kubernetes Deployment

Create `k8s-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: livestream-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: livestream-service
  template:
    metadata:
      labels:
        app: livestream-service
    spec:
      containers:
      - name: livestream-service
        image: livestreamservice:latest
        ports:
        - containerPort: 8080
        env:
        - name: SPRING_DATASOURCE_URL
          valueFrom:
            secretKeyRef:
              name: livestream-secrets
              key: db-url
        - name: SPRING_DATASOURCE_USERNAME
          valueFrom:
            secretKeyRef:
              name: livestream-secrets
              key: db-username
        - name: SPRING_DATASOURCE_PASSWORD
          valueFrom:
            secretKeyRef:
              name: livestream-secrets
              key: db-password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: livestream-secrets
              key: jwt-secret
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1024Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: livestream-service
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 8080
  selector:
    app: livestream-service
```

Deploy:
```bash
kubectl apply -f k8s-deployment.yaml
```

## Environment Configuration

### Environment Variables

The application supports these environment variables:

```bash
# Server
SERVER_PORT=8080

# Database
SPRING_DATASOURCE_URL=jdbc:sqlserver://localhost:1433;databaseName=DBDACS_Livestream
SPRING_DATASOURCE_USERNAME=sa
SPRING_DATASOURCE_PASSWORD=YourPassword123

# JWT
JWT_SECRET=your-very-long-secret-key
JWT_EXPIRATION=86400000

# CORS
CORS_ALLOWED_ORIGINS=http://localhost:5000,http://localhost:5001

# Logging
LOGGING_LEVEL_ROOT=INFO
LOGGING_LEVEL_COM_EXAMPLE_LIVESTREAMSERVICE=DEBUG
```

### Application Profiles

#### Production Profile

Create `application-prod.properties`:

```properties
server.port=8080

# Database
spring.datasource.url=${SPRING_DATASOURCE_URL}
spring.datasource.username=${SPRING_DATASOURCE_USERNAME}
spring.datasource.password=${SPRING_DATASOURCE_PASSWORD}

# JPA
spring.jpa.hibernate.ddl-auto=validate
spring.jpa.show-sql=false

# JWT
jwt.secret=${JWT_SECRET}
jwt.expiration=86400000

# Logging
logging.level.root=WARN
logging.level.com.example.livestreamservice=INFO

# CORS
cors.allowed-origins=${CORS_ALLOWED_ORIGINS}
```

Run with profile:
```bash
java -jar -Dspring.profiles.active=prod livestreamservice.jar
```

### SSL/HTTPS Configuration

Add to `application.properties`:

```properties
server.port=8443
server.ssl.key-store=classpath:keystore.p12
server.ssl.key-store-password=your-keystore-password
server.ssl.key-store-type=PKCS12
server.ssl.key-alias=livestream
```

Generate keystore:
```bash
keytool -genkeypair -alias livestream -keyalg RSA -keysize 2048 \
  -storetype PKCS12 -keystore keystore.p12 -validity 3650
```

## Health Check Endpoints

Spring Boot Actuator endpoints (add dependency if needed):

```properties
management.endpoints.web.exposure.include=health,info
management.endpoint.health.show-details=always
```

Health check: `GET /actuator/health`

## Monitoring and Logging

### Logging Configuration

```properties
# File logging
logging.file.name=logs/livestream-service.log
logging.file.max-size=10MB
logging.file.max-history=30

# Pattern
logging.pattern.console=%d{yyyy-MM-dd HH:mm:ss} - %msg%n
logging.pattern.file=%d{yyyy-MM-dd HH:mm:ss} [%thread] %-5level %logger{36} - %msg%n
```

### Application Insights (Azure)

Add dependency and configure:

```properties
azure.application-insights.instrumentation-key=your-key
```

## Backup and Recovery

### Database Backup

```sql
BACKUP DATABASE DBDACS_Livestream 
TO DISK = 'C:\Backups\DBDACS_Livestream.bak'
WITH FORMAT, COMPRESSION;
```

### Automated Backup Script

Windows:
```batch
@echo off
sqlcmd -S localhost -U sa -P YourPassword123 -Q "BACKUP DATABASE DBDACS_Livestream TO DISK = 'C:\Backups\DBDACS_Livestream_%date:~-4,4%%date:~-10,2%%date:~-7,2%.bak' WITH FORMAT, COMPRESSION"
```

Linux:
```bash
#!/bin/bash
DATE=$(date +%Y%m%d)
sqlcmd -S localhost -U sa -P YourPassword123 -Q "BACKUP DATABASE DBDACS_Livestream TO DISK = '/backups/DBDACS_Livestream_$DATE.bak' WITH FORMAT, COMPRESSION"
```

## Scaling Considerations

### Horizontal Scaling
- Stateless design allows multiple instances
- Use load balancer (Nginx, HAProxy, Azure Load Balancer)
- Shared database across all instances
- Consider Redis for session management

### Load Balancing Example (Nginx)

```nginx
upstream livestream_backend {
    server 127.0.0.1:8080;
    server 127.0.0.1:8081;
    server 127.0.0.1:8082;
}

server {
    listen 80;
    server_name livestream.example.com;

    location / {
        proxy_pass http://livestream_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    location /ws {
        proxy_pass http://livestream_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

## Troubleshooting

### Check Application Status
```bash
curl http://localhost:8080/api/rooms/active
```

### View Logs
```bash
# Docker
docker logs livestream-service

# Linux service
journalctl -u livestream-service -f

# Windows
type C:\livestream-service\logs\service.log
```

### Common Issues

1. **Port already in use**: Change `server.port` in configuration
2. **Database connection failed**: Check connection string and credentials
3. **Out of memory**: Increase heap size with `-Xmx` flag
4. **WebSocket not working**: Check proxy configuration for WebSocket support

## Security Checklist

- [ ] Use HTTPS in production
- [ ] Change default JWT secret
- [ ] Use environment variables for sensitive data
- [ ] Enable firewall rules
- [ ] Use strong database passwords
- [ ] Regular security updates
- [ ] Enable audit logging
- [ ] Implement rate limiting
- [ ] Regular backups
- [ ] Monitor for suspicious activity

## Summary

This deployment guide covers various deployment scenarios. Choose the one that best fits your infrastructure:

- **Development**: `mvn spring-boot:run`
- **Simple Production**: JAR file + NSSM on Windows
- **Cloud**: Azure App Service or Container Instances
- **Containerized**: Docker or Kubernetes
- **High Availability**: Multiple instances with load balancer

For production deployments, always:
1. Use environment variables for configuration
2. Enable HTTPS
3. Implement proper monitoring
4. Set up automated backups
5. Use strong passwords and secrets
