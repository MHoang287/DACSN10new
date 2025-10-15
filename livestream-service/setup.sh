#!/bin/bash

# Livestream Service Setup Script
# This script helps set up the Spring Boot livestream service

echo "=========================================="
echo "Livestream Service Setup"
echo "=========================================="
echo ""

# Check Java installation
echo "Checking Java installation..."
if ! command -v java &> /dev/null; then
    echo "❌ Java is not installed. Please install JDK 17 or higher."
    exit 1
fi

JAVA_VERSION=$(java -version 2>&1 | awk -F '"' '/version/ {print $2}' | cut -d'.' -f1)
if [ "$JAVA_VERSION" -lt 17 ]; then
    echo "❌ Java version $JAVA_VERSION is too old. Please install JDK 17 or higher."
    exit 1
fi
echo "✅ Java $JAVA_VERSION detected"
echo ""

# Check Maven installation
echo "Checking Maven installation..."
if ! command -v mvn &> /dev/null; then
    echo "❌ Maven is not installed. Please install Maven 3.6 or higher."
    exit 1
fi
echo "✅ Maven detected"
echo ""

# Database configuration
echo "=========================================="
echo "Database Configuration"
echo "=========================================="
echo ""
echo "Please enter your MS SQL Server connection details:"
read -p "Server host (default: localhost): " DB_HOST
DB_HOST=${DB_HOST:-localhost}

read -p "Server port (default: 1433): " DB_PORT
DB_PORT=${DB_PORT:-1433}

read -p "Database name (default: DBDACS_Livestream): " DB_NAME
DB_NAME=${DB_NAME:-DBDACS_Livestream}

read -p "Username (default: sa): " DB_USER
DB_USER=${DB_USER:-sa}

read -sp "Password: " DB_PASS
echo ""

read -p "Use integrated security/Windows Authentication? (y/n, default: n): " USE_INTEGRATED
USE_INTEGRATED=${USE_INTEGRATED:-n}

# JWT Secret
echo ""
echo "=========================================="
echo "JWT Configuration"
echo "=========================================="
echo ""
read -p "Enter JWT secret key (press Enter to generate random): " JWT_SECRET
if [ -z "$JWT_SECRET" ]; then
    JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')
    echo "Generated JWT secret: $JWT_SECRET"
fi

# Update application.properties
echo ""
echo "Updating application.properties..."

if [ "$USE_INTEGRATED" = "y" ] || [ "$USE_INTEGRATED" = "Y" ]; then
    DB_URL="jdbc:sqlserver://${DB_HOST}:${DB_PORT};databaseName=${DB_NAME};integratedSecurity=true;trustServerCertificate=true"
    cat > src/main/resources/application.properties << EOF
# Server Configuration
spring.application.name=livestreamservice
server.port=8080

# Database Configuration (MS SQL Server with Integrated Security)
spring.datasource.url=${DB_URL}
spring.datasource.driver-class-name=com.microsoft.sqlserver.jdbc.SQLServerDriver

# JPA Configuration
spring.jpa.hibernate.ddl-auto=update
spring.jpa.show-sql=true
spring.jpa.properties.hibernate.format_sql=true
spring.jpa.properties.hibernate.dialect=org.hibernate.dialect.SQLServerDialect

# JWT Configuration
jwt.secret=${JWT_SECRET}
jwt.expiration=86400000

# Logging
logging.level.root=INFO
logging.level.com.example.livestreamservice=DEBUG
logging.level.org.springframework.security=DEBUG

# CORS Configuration
cors.allowed-origins=http://localhost:5000,http://localhost:5001,https://localhost:7001
EOF
else
    DB_URL="jdbc:sqlserver://${DB_HOST}:${DB_PORT};databaseName=${DB_NAME};encrypt=true;trustServerCertificate=true"
    cat > src/main/resources/application.properties << EOF
# Server Configuration
spring.application.name=livestreamservice
server.port=8080

# Database Configuration (MS SQL Server)
spring.datasource.url=${DB_URL}
spring.datasource.username=${DB_USER}
spring.datasource.password=${DB_PASS}
spring.datasource.driver-class-name=com.microsoft.sqlserver.jdbc.SQLServerDriver

# JPA Configuration
spring.jpa.hibernate.ddl-auto=update
spring.jpa.show-sql=true
spring.jpa.properties.hibernate.format_sql=true
spring.jpa.properties.hibernate.dialect=org.hibernate.dialect.SQLServerDialect

# JWT Configuration
jwt.secret=${JWT_SECRET}
jwt.expiration=86400000

# Logging
logging.level.root=INFO
logging.level.com.example.livestreamservice=DEBUG
logging.level.org.springframework.security=DEBUG

# CORS Configuration
cors.allowed-origins=http://localhost:5000,http://localhost:5001,https://localhost:7001
EOF
fi

echo "✅ Configuration updated"
echo ""

# Install dependencies
echo "=========================================="
echo "Installing Dependencies"
echo "=========================================="
echo ""
echo "Running: mvn clean install"
mvn clean install -DskipTests

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Dependencies installed successfully"
else
    echo ""
    echo "❌ Failed to install dependencies"
    exit 1
fi

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "To start the application, run:"
echo "  mvn spring-boot:run"
echo ""
echo "Or build and run the JAR:"
echo "  mvn package"
echo "  java -jar target/livestreamservice-0.0.1-SNAPSHOT.jar"
echo ""
echo "The service will be available at: http://localhost:8080"
echo ""
echo "API Documentation: See README.md for API endpoints"
echo ""
