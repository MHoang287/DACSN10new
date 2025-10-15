# Ngrok Setup Guide for Livestream Service

This guide explains how to use Ngrok to expose your local livestream service to the internet, enabling testing with teachers and students on different networks.

## What is Ngrok?

Ngrok is a cross-platform application that creates secure tunnels from a public endpoint to a locally running web service. It's perfect for:
- Testing webhooks and APIs
- Demonstrating applications without deployment
- Testing on different networks (teacher at home, student at school)
- Mobile device testing

## Prerequisites

1. Spring Boot Livestream Service running locally
2. Ngrok account (free tier is sufficient)
3. Ngrok installed on your machine

## Installation

### Windows

1. Download Ngrok from: https://ngrok.com/download
2. Extract the ZIP file to a folder (e.g., `C:\ngrok`)
3. Add the folder to your PATH (optional)

### macOS

```bash
# Using Homebrew
brew install ngrok/ngrok/ngrok

# Or download from website
curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null
```

### Linux

```bash
# Download
wget https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-linux-amd64.tgz
tar xvzf ngrok-v3-stable-linux-amd64.tgz
sudo mv ngrok /usr/local/bin/
```

## Setup

### 1. Sign Up for Ngrok Account

1. Visit: https://dashboard.ngrok.com/signup
2. Create a free account
3. Get your authentication token from: https://dashboard.ngrok.com/get-started/your-authtoken

### 2. Configure Ngrok

Run this command once to add your authtoken:

```bash
ngrok config add-authtoken YOUR_AUTH_TOKEN_HERE
```

## Basic Usage

### Exposing the Livestream Service

1. **Start your Spring Boot application**:
   ```bash
   cd livestream-service
   mvn spring-boot:run
   ```

2. **In a new terminal, start Ngrok**:
   ```bash
   ngrok http 8080
   ```

3. **Ngrok will display**:
   ```
   Session Status                online
   Account                       your-email@example.com
   Version                       3.x.x
   Region                        United States (us)
   Latency                       -
   Web Interface                 http://127.0.0.1:4040
   Forwarding                    https://abc123def456.ngrok-free.app -> http://localhost:8080
   ```

4. **Use the Forwarding URL**: The HTTPS URL (e.g., `https://abc123def456.ngrok-free.app`) is your public URL.

## Testing Scenario: Teacher and Student on Different Networks

### Scenario Setup

- **Teacher**: At home with their computer
- **Student**: At school or another location
- **Goal**: Teacher livestreams a class, student watches remotely

### Step 1: Teacher Setup

1. Teacher starts the Spring Boot service on their computer:
   ```bash
   mvn spring-boot:run
   ```

2. Teacher starts Ngrok:
   ```bash
   ngrok http 8080
   ```

3. Teacher notes the public URL (e.g., `https://abc123.ngrok-free.app`)

4. Teacher shares this URL with students

### Step 2: Teacher Creates Room

Teacher uses the public URL to create a livestream room:

```bash
# Register/Login
curl -X POST https://abc123.ngrok-free.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teacher1",
    "password": "password123"
  }'

# Save the token from response

# Create room
curl -X POST https://abc123.ngrok-free.app/api/rooms/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "roomName": "Math Class - Algebra",
    "description": "Live algebra lesson"
  }'
```

### Step 3: Student Setup

1. Student receives the Ngrok URL from teacher
2. Student can access the service from anywhere:
   ```
   https://abc123.ngrok-free.app
   ```

### Step 4: Student Joins Room

```bash
# Register/Login
curl -X POST https://abc123.ngrok-free.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "student1",
    "password": "password123"
  }'

# Get active rooms
curl https://abc123.ngrok-free.app/api/rooms/active

# Join room
curl -X POST https://abc123.ngrok-free.app/api/rooms/ROOM_ID/join \
  -H "Authorization: Bearer STUDENT_TOKEN"
```

## Using Ngrok with ASP.NET MVC Integration

### Update ASP.NET MVC Configuration

In your ASP.NET MVC `appsettings.json`:

```json
{
  "LivestreamService": {
    "BaseUrl": "https://abc123.ngrok-free.app/api"
  }
}
```

### Update CORS in Spring Boot

Update `SecurityConfig.java` to allow the Ngrok domain:

```java
configuration.setAllowedOrigins(Arrays.asList(
    "http://localhost:5000",
    "http://localhost:5001", 
    "https://localhost:7001",
    "https://abc123.ngrok-free.app"  // Add your ngrok URL
));
```

Or update `application.properties`:

```properties
cors.allowed-origins=http://localhost:5000,https://abc123.ngrok-free.app
```

## Advanced Ngrok Features

### 1. Custom Subdomain (Paid Plans)

```bash
ngrok http 8080 --subdomain=mylivestreamservice
```

This gives you: `https://mylivestreamservice.ngrok.io`

### 2. Multiple Tunnels

Create `ngrok.yml`:

```yaml
tunnels:
  livestream-api:
    proto: http
    addr: 8080
  asp-mvc:
    proto: http
    addr: 5000
```

Start all tunnels:
```bash
ngrok start --all
```

### 3. Inspect Traffic

Open the Ngrok web interface at: `http://127.0.0.1:4040`

This shows:
- All HTTP requests
- Request/response details
- Replay requests
- Request statistics

### 4. Regional Endpoints

```bash
# Use different regions for better latency
ngrok http 8080 --region eu    # Europe
ngrok http 8080 --region ap    # Asia Pacific
ngrok http 8080 --region au    # Australia
ngrok http 8080 --region sa    # South America
ngrok http 8080 --region jp    # Japan
ngrok http 8080 --region in    # India
```

### 5. Basic Authentication

Protect your tunnel with basic auth:

```bash
ngrok http 8080 --auth="username:password"
```

### 6. Custom Domain (Paid Plans)

```bash
ngrok http 8080 --hostname=myservice.example.com
```

## Testing Checklist

### Before Testing

- [ ] Spring Boot service is running locally
- [ ] Ngrok is connected and showing public URL
- [ ] CORS is configured to allow the Ngrok domain
- [ ] Firewall allows Ngrok connections
- [ ] Test the public URL with curl or browser

### Teacher Checklist

- [ ] Register/login to livestream service
- [ ] Create a room successfully
- [ ] Start local camera/microphone
- [ ] Connect to WebSocket
- [ ] Share room ID or public URL with students

### Student Checklist

- [ ] Access the public Ngrok URL
- [ ] Register/login to livestream service
- [ ] View active rooms
- [ ] Join teacher's room
- [ ] Connect to WebSocket
- [ ] Receive video stream

## Troubleshooting

### Issue: "Failed to complete tunnel connection"

**Solution**: Check your internet connection and firewall settings.

### Issue: "Invalid Host Header"

**Solution**: This is usually not an issue with Spring Boot. If it occurs, add to `application.properties`:

```properties
server.forward-headers-strategy=native
```

### Issue: CORS errors when using Ngrok URL

**Solution**: Update CORS configuration in `SecurityConfig.java` to include your Ngrok domain.

### Issue: WebSocket connection fails

**Solution**: 
1. Ensure WebSocket endpoint is accessible: `wss://your-ngrok-url/ws`
2. Check browser console for connection errors
3. Verify Ngrok supports WebSocket (it does by default)

### Issue: Ngrok tunnel expires

**Solution**: 
- Free tier tunnels expire after 2 hours
- Restart Ngrok to get a new URL
- Consider upgrading to a paid plan for persistent URLs

### Issue: Performance is slow

**Solution**:
- Use a regional endpoint closer to your location
- Check your local internet connection
- Ngrok free tier has bandwidth limitations

## Security Considerations

### Production Use

⚠️ **Ngrok is great for testing but NOT recommended for production**:
- URLs change frequently (free tier)
- Bandwidth limitations
- No guaranteed uptime
- Security depends on Ngrok's infrastructure

### For Production

Instead of Ngrok, consider:
- Proper cloud hosting (Azure, AWS, Google Cloud)
- Domain name with SSL certificate
- Load balancing and CDN
- Professional infrastructure

### Security Best Practices with Ngrok

1. **Don't expose sensitive data** without authentication
2. **Use HTTPS** (Ngrok provides this by default)
3. **Implement rate limiting** in your application
4. **Monitor the Ngrok inspector** for suspicious activity
5. **Change URLs regularly** if using free tier
6. **Use authentication tokens** for API access
7. **Don't share Ngrok URLs publicly** for extended periods

## Example: Complete Testing Flow

### Terminal 1: Start Spring Boot

```bash
cd livestream-service
mvn spring-boot:run
```

### Terminal 2: Start Ngrok

```bash
ngrok http 8080
```

**Copy the HTTPS URL**: `https://abc123.ngrok-free.app`

### Terminal 3: Teacher Actions

```bash
# Set the Ngrok URL
NGROK_URL="https://abc123.ngrok-free.app"

# Register teacher
curl -X POST $NGROK_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teacher1",
    "password": "secure123",
    "email": "teacher@school.edu",
    "fullName": "John Teacher",
    "role": "TEACHER"
  }'

# Create room (use token from above)
curl -X POST $NGROK_URL/api/rooms/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TEACHER_TOKEN" \
  -d '{
    "roomName": "Physics 101",
    "description": "Introduction to mechanics"
  }'
```

### Terminal 4: Student Actions (Different Machine)

```bash
# Set the Ngrok URL (received from teacher)
NGROK_URL="https://abc123.ngrok-free.app"

# Register student
curl -X POST $NGROK_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "student1",
    "password": "secure123",
    "email": "student@school.edu",
    "fullName": "Jane Student",
    "role": "STUDENT"
  }'

# View active rooms
curl $NGROK_URL/api/rooms/active

# Join room (use token and room ID from above)
curl -X POST $NGROK_URL/api/rooms/ROOM_ID_HERE/join \
  -H "Authorization: Bearer YOUR_STUDENT_TOKEN"
```

## Ngrok Alternatives

If Ngrok doesn't work for you, consider:

1. **LocalTunnel**: https://localtunnel.github.io/www/
2. **Serveo**: https://serveo.net/
3. **Telebit**: https://telebit.cloud/
4. **PageKite**: https://pagekite.net/
5. **Cloudflare Tunnel**: https://www.cloudflare.com/products/tunnel/

## Support

- Ngrok Documentation: https://ngrok.com/docs
- Ngrok Community: https://ngrok.com/slack
- Spring Boot WebRTC: https://webrtc.org/getting-started/overview

## Summary

Ngrok is an excellent tool for testing your livestream service across different networks. It allows teachers and students to connect from anywhere without complex network configuration. Remember to use it for testing only and deploy to proper infrastructure for production use.
