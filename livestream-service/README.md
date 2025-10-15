# Livestream Service - Spring Boot WebRTC Application

This is a separate Spring Boot service that provides livestream functionality for teachers in the ASP.NET MVC website. It uses WebRTC protocol for real-time video streaming, WebSockets for signaling, JWT for authentication, and MS SQL Server for data persistence.

## Features

- **Multiple Simultaneous Livestreams**: Each teacher can create their own livestream room
- **WebRTC Streaming**: Real-time video/audio streaming using WebRTC protocol
- **WebSocket Signaling**: Real-time communication for WebRTC offer/answer/ICE candidates
- **JWT Authentication**: Secure authentication for teachers and students
- **MS SQL Server Database**: Persistent storage for users and rooms
- **Spring Security**: Secured endpoints with role-based access control
- **REST APIs**: Integration with ASP.NET MVC website

## Technology Stack

- **Java 17** (or Java 21)
- **Spring Boot 3.5.6**
- **Spring Web** - REST API
- **Spring WebSocket** - Real-time communication
- **Spring Data JPA** - Database access
- **Spring Security** - Authentication and authorization
- **JWT (jjwt 0.12.6)** - Token-based authentication
- **MS SQL Server** - Database
- **Lombok** - Code generation
- **Maven** - Build tool

## Prerequisites

Before setting up the project, ensure you have the following installed:

1. **Java Development Kit (JDK) 17 or 21**
   - Download from: https://www.oracle.com/java/technologies/downloads/
   - Or use OpenJDK: https://adoptium.net/

2. **Maven 3.6+**
   - Download from: https://maven.apache.org/download.cgi
   - Or install via package manager

3. **MS SQL Server**
   - SQL Server 2019 or later
   - SQL Server Express is sufficient for development

4. **Visual Studio Code (VS Code)**
   - Download from: https://code.visualstudio.com/

5. **VS Code Extensions**
   - Extension Pack for Java
   - Spring Boot Extension Pack
   - Maven for Java

## Setup Instructions

### 1. Create Spring Boot Project in VS Code

#### Option A: Using Spring Initializr in VS Code

1. Open VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac) to open Command Palette
3. Type "Spring Initializr" and select "Spring Initializr: Create a Maven Project"
4. Follow the prompts:
   - Spring Boot Version: `3.5.6`
   - Language: `Java`
   - Group Id: `com.example`
   - Artifact Id: `livestreamservice`
   - Packaging: `Jar`
   - Java Version: `17` (or `21`)
5. Select dependencies:
   - Spring Web
   - WebSocket
   - Spring Data JPA
   - Spring Security
   - Validation
   - MS SQL Server Driver
   - Lombok

#### Option B: Using Existing Project

Since the project is already created, you can:

1. Open VS Code
2. Navigate to File > Open Folder
3. Select the `livestream-service` directory

### 2. Configure Database Connection

1. **Create Database**:
   ```sql
   CREATE DATABASE DBDACS_Livestream;
   ```

2. **Update `application.properties`**:
   
   Edit `src/main/resources/application.properties`:
   
   ```properties
   # Update these values according to your SQL Server configuration
   spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=DBDACS_Livestream;encrypt=true;trustServerCertificate=true
   spring.datasource.username=YOUR_SQL_USERNAME
   spring.datasource.password=YOUR_SQL_PASSWORD
   
   # JWT Secret (change this to a strong secret key)
   jwt.secret=yourVeryLongSecretKeyThatShouldBeAtLeast256BitsLongForHS256Algorithm
   ```

3. **For Integrated Security (Windows Authentication)**:
   ```properties
   spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=DBDACS_Livestream;integratedSecurity=true;trustServerCertificate=true
   ```

### 3. Install Dependencies

Open terminal in VS Code and run:

```bash
cd livestream-service
mvn clean install
```

This will download all dependencies defined in `pom.xml`.

### 4. Run the Application

#### Option A: Using VS Code

1. Open `LivestreamServiceApplication.java`
2. Right-click in the file
3. Select "Run Java"

#### Option B: Using Maven

```bash
mvn spring-boot:run
```

#### Option C: Using Terminal

```bash
# Build the JAR file
mvn clean package

# Run the JAR
java -jar target/livestreamservice-0.0.1-SNAPSHOT.jar
```

The application will start on `http://localhost:8080`

### 5. Verify Application is Running

Open a browser or use curl:

```bash
curl http://localhost:8080/api/auth/login
```

You should see a response (even if it's an error, it confirms the service is running).

## API Documentation

### Authentication APIs

#### 1. Register User (Teacher or Student)

**Endpoint**: `POST /api/auth/register`

**Request Body**:
```json
{
  "username": "teacher1",
  "password": "password123",
  "email": "teacher1@example.com",
  "fullName": "John Doe",
  "role": "TEACHER"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "type": "Bearer",
  "id": 1,
  "username": "teacher1",
  "email": "teacher1@example.com",
  "fullName": "John Doe",
  "role": "TEACHER"
}
```

#### 2. Login

**Endpoint**: `POST /api/auth/login`

**Request Body**:
```json
{
  "username": "teacher1",
  "password": "password123"
}
```

**Response**: Same as register response

### Livestream Room APIs

#### 3. Create Room (Teacher Only)

**Endpoint**: `POST /api/rooms/create`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

**Request Body**:
```json
{
  "roomName": "Math Class - Introduction to Algebra",
  "description": "Learn the basics of algebra",
  "maxParticipants": 50
}
```

**Response**:
```json
{
  "id": 1,
  "roomId": "550e8400-e29b-41d4-a716-446655440000",
  "roomName": "Math Class - Introduction to Algebra",
  "description": "Learn the basics of algebra",
  "teacherName": "John Doe",
  "teacherId": 1,
  "status": "ACTIVE",
  "createdAt": "2025-10-15T10:30:00",
  "participantCount": 0,
  "maxParticipants": 50
}
```

#### 4. Get Active Rooms

**Endpoint**: `GET /api/rooms/active`

**Response**:
```json
[
  {
    "id": 1,
    "roomId": "550e8400-e29b-41d4-a716-446655440000",
    "roomName": "Math Class",
    "teacherName": "John Doe",
    "participantCount": 5,
    "maxParticipants": 50,
    "status": "ACTIVE"
  }
]
```

#### 5. Join Room (Student)

**Endpoint**: `POST /api/rooms/{roomId}/join`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

#### 6. Leave Room

**Endpoint**: `POST /api/rooms/{roomId}/leave`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

#### 7. End Room (Teacher Only)

**Endpoint**: `POST /api/rooms/{roomId}/end`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

### WebSocket Signaling

Connect to WebSocket endpoint: `ws://localhost:8080/ws`

**Topics**:
- `/topic/signal/{roomId}` - Broadcast messages to all room participants
- `/queue/signal/{userId}` - Direct messages to specific user

**Send Messages to**:
- `/app/signal/{roomId}` - Send signaling messages (offer, answer, ICE candidates)
- `/app/join/{roomId}` - Join room notification
- `/app/leave/{roomId}` - Leave room notification

**Message Format**:
```json
{
  "type": "offer",
  "roomId": "550e8400-e29b-41d4-a716-446655440000",
  "from": "teacher1",
  "to": "student1",
  "data": {
    "sdp": "v=0\r\no=- ...",
    "type": "offer"
  }
}
```

## Using Ngrok for Testing Across Networks

Ngrok allows you to expose your local server to the internet, enabling testing with teacher and student on different machines/networks.

### 1. Install Ngrok

Download from: https://ngrok.com/download

### 2. Start Your Spring Boot Application

```bash
mvn spring-boot:run
```

### 3. Start Ngrok

```bash
ngrok http 8080
```

### 4. Use the Ngrok URL

Ngrok will provide a public URL like: `https://abc123.ngrok.io`

Use this URL instead of `http://localhost:8080` when making API calls from other machines.

**Example**:
```
https://abc123.ngrok.io/api/auth/login
```

### 5. Update CORS Settings

If using a different domain, update CORS in `SecurityConfig.java`:

```java
configuration.setAllowedOrigins(Arrays.asList(
    "http://localhost:5000",
    "https://abc123.ngrok.io"  // Add your ngrok URL
));
```

## Integration with ASP.NET MVC Website

### From ASP.NET MVC, Call the APIs

**Example in C#**:

```csharp
public class LivestreamService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:8080/api";
    
    public async Task<string> AuthenticateTeacher(string username, string password)
    {
        var loginData = new { username, password };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/auth/login", loginData);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result.Token;
    }
    
    public async Task<RoomResponse> CreateLivestream(string token, CreateRoomRequest request)
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/rooms/create", request);
        return await response.Content.ReadFromJsonAsync<RoomResponse>();
    }
}
```

### WebSocket Connection from Browser

Include in your ASP.NET MVC view:

```html
<script src="https://cdn.jsdelivr.net/npm/sockjs-client@1/dist/sockjs.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/stompjs@2.3.3/lib/stomp.min.js"></script>

<script>
    const socket = new SockJS('http://localhost:8080/ws');
    const stompClient = Stomp.over(socket);
    
    stompClient.connect({}, function(frame) {
        console.log('Connected: ' + frame);
        
        // Subscribe to room signals
        stompClient.subscribe('/topic/signal/' + roomId, function(message) {
            const signal = JSON.parse(message.body);
            handleSignal(signal);
        });
    });
    
    // Send offer/answer/ICE candidate
    function sendSignal(type, data) {
        stompClient.send('/app/signal/' + roomId, {}, JSON.stringify({
            type: type,
            from: username,
            to: recipient,
            data: data
        }));
    }
</script>
```

## Testing the Application

### 1. Register a Teacher

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teacher1",
    "password": "password123",
    "email": "teacher1@example.com",
    "fullName": "John Doe",
    "role": "TEACHER"
  }'
```

### 2. Login as Teacher

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "teacher1",
    "password": "password123"
  }'
```

Save the token from the response.

### 3. Create a Room

```bash
curl -X POST http://localhost:8080/api/rooms/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "roomName": "Math Class",
    "description": "Algebra basics",
    "maxParticipants": 50
  }'
```

### 4. Register a Student

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "student1",
    "password": "password123",
    "email": "student1@example.com",
    "fullName": "Jane Smith",
    "role": "STUDENT"
  }'
```

### 5. Join Room as Student

```bash
curl -X POST http://localhost:8080/api/rooms/ROOM_ID/join \
  -H "Authorization: Bearer STUDENT_TOKEN_HERE"
```

## Troubleshooting

### 1. Port Already in Use

If port 8080 is already in use, change it in `application.properties`:
```properties
server.port=8081
```

### 2. Database Connection Errors

- Verify SQL Server is running
- Check SQL Server configuration allows TCP/IP connections
- Verify database name, username, and password are correct
- Check firewall settings

### 3. JWT Token Errors

- Ensure the `jwt.secret` in `application.properties` is at least 256 bits (32 characters)
- Verify the token is being sent in the Authorization header as "Bearer TOKEN"

### 4. Maven Build Errors

Clear Maven cache and rebuild:
```bash
mvn clean
mvn install -U
```

### 5. Java Version Issues

Verify Java version:
```bash
java -version
```

Should show Java 17 or higher.

## Project Structure

```
livestream-service/
├── src/
│   ├── main/
│   │   ├── java/com/example/livestreamservice/
│   │   │   ├── config/              # Configuration classes
│   │   │   │   ├── SecurityConfig.java
│   │   │   │   └── WebSocketConfig.java
│   │   │   ├── controller/          # REST controllers
│   │   │   │   ├── AuthController.java
│   │   │   │   └── LivestreamRoomController.java
│   │   │   ├── dto/                 # Data Transfer Objects
│   │   │   │   ├── AuthResponse.java
│   │   │   │   ├── CreateRoomRequest.java
│   │   │   │   ├── LoginRequest.java
│   │   │   │   ├── RegisterRequest.java
│   │   │   │   ├── RoomResponse.java
│   │   │   │   └── SignalingMessage.java
│   │   │   ├── model/               # JPA entities
│   │   │   │   ├── Teacher.java
│   │   │   │   ├── Student.java
│   │   │   │   └── LivestreamRoom.java
│   │   │   ├── repository/          # JPA repositories
│   │   │   │   ├── TeacherRepository.java
│   │   │   │   ├── StudentRepository.java
│   │   │   │   └── LivestreamRoomRepository.java
│   │   │   ├── security/            # Security components
│   │   │   │   ├── JwtUtils.java
│   │   │   │   └── JwtAuthenticationFilter.java
│   │   │   ├── service/             # Business logic
│   │   │   │   ├── AuthService.java
│   │   │   │   └── LivestreamRoomService.java
│   │   │   ├── websocket/           # WebSocket handlers
│   │   │   │   └── SignalingController.java
│   │   │   └── LivestreamServiceApplication.java
│   │   └── resources/
│   │       └── application.properties
│   └── test/                        # Test files
├── pom.xml                          # Maven configuration
└── README.md                        # This file
```

## Security Considerations

1. **Change JWT Secret**: In production, use a strong, randomly generated secret key
2. **HTTPS**: Use HTTPS in production (configure SSL in application.properties)
3. **CORS**: Restrict CORS origins to only trusted domains
4. **Database**: Use strong database credentials and secure connection strings
5. **Rate Limiting**: Consider implementing rate limiting for API endpoints
6. **Input Validation**: All inputs are validated using Jakarta Validation annotations

## Next Steps

1. Implement frontend UI for teacher and student interfaces
2. Add recording functionality for livestreams
3. Implement chat functionality during livestreams
4. Add analytics and reporting features
5. Implement load balancing for production deployment
6. Add unit and integration tests

## Support

For issues or questions:
- Check the troubleshooting section
- Review Spring Boot documentation: https://spring.io/projects/spring-boot
- Review WebRTC documentation: https://webrtc.org/

## License

This project is part of the DACSN10new educational platform.
