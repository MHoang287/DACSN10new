# Quick Start Guide - Livestream Service

Get up and running with the livestream service in under 10 minutes!

## Prerequisites Checklist

- [ ] Java 17 or higher installed
- [ ] Maven 3.6+ installed
- [ ] MS SQL Server running
- [ ] Visual Studio Code (optional but recommended)

## 5-Minute Setup

### 1. Configure Database (2 minutes)

Open `src/main/resources/application.properties` and update:

```properties
spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=DBDACS_Livestream;encrypt=true;trustServerCertificate=true
spring.datasource.username=YOUR_USERNAME
spring.datasource.password=YOUR_PASSWORD
```

**Or use the setup script**:
```bash
chmod +x setup.sh
./setup.sh
```

### 2. Build the Project (2 minutes)

```bash
cd livestream-service
mvn clean install
```

### 3. Run the Application (1 minute)

```bash
mvn spring-boot:run
```

The service will start at: `http://localhost:8080`

## Test the Service (5 minutes)

### Test 1: Health Check

```bash
curl http://localhost:8080/api/rooms/active
```

Expected: `[]` (empty array, no active rooms)

### Test 2: Register a Teacher

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teacher1",
    "password": "password123",
    "email": "teacher1@school.edu",
    "fullName": "John Doe",
    "role": "TEACHER"
  }'
```

Expected: JSON response with token

### Test 3: Create a Room

```bash
# Save the token from step 2
TOKEN="YOUR_TOKEN_HERE"

curl -X POST http://localhost:8080/api/rooms/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "roomName": "Test Room",
    "description": "My first livestream"
  }'
```

Expected: JSON response with room details

### Test 4: View Active Rooms

```bash
curl http://localhost:8080/api/rooms/active
```

Expected: JSON array with your created room

## Test with Web Interface

1. Open `example-client.html` in a browser
2. Register a teacher account
3. Create a room
4. Start local video
5. In another browser/tab, register as student and join

## What's Next?

### For Development
- Read the [README.md](README.md) for full documentation
- Check [AspNetMvcIntegration.md](AspNetMvcIntegration.md) for integration guide
- Review API endpoints and examples

### For Testing Across Networks
- Follow [NgrokSetupGuide.md](NgrokSetupGuide.md) to expose your service
- Test with teacher on one network, student on another

### For Production
- Change JWT secret to a strong random value
- Use HTTPS
- Configure proper CORS origins
- Set up proper database credentials
- Implement rate limiting
- Add monitoring and logging

## Troubleshooting

### "Port 8080 already in use"

Change port in `application.properties`:
```properties
server.port=8081
```

### "Cannot connect to database"

1. Verify SQL Server is running
2. Check credentials in `application.properties`
3. Ensure database exists: `CREATE DATABASE DBDACS_Livestream;`

### "Build fails"

```bash
# Clear and rebuild
mvn clean
mvn install -U
```

### Need Help?

- Check the full [README.md](README.md)
- Review error logs in the console
- Check database connection settings

## Success!

If you've completed all tests successfully, you now have:
- ✅ A running livestream service
- ✅ REST APIs for authentication and room management
- ✅ WebSocket support for real-time signaling
- ✅ Database persistence
- ✅ JWT authentication

Ready to integrate with your ASP.NET MVC website!
