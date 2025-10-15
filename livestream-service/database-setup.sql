-- Livestream Service Database Setup Script
-- This script creates the database and initial schema for the livestream service
-- Run this in SQL Server Management Studio or Azure Data Studio

-- Create the database
USE master;
GO

-- Drop database if exists (BE CAREFUL - this will delete all data!)
-- Uncomment the line below only if you want to recreate the database
-- DROP DATABASE IF EXISTS DBDACS_Livestream;
-- GO

-- Create new database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DBDACS_Livestream')
BEGIN
    CREATE DATABASE DBDACS_Livestream;
    PRINT 'Database DBDACS_Livestream created successfully.';
END
ELSE
BEGIN
    PRINT 'Database DBDACS_Livestream already exists.';
END
GO

-- Use the database
USE DBDACS_Livestream;
GO

-- Note: The actual tables will be created automatically by Spring Boot JPA
-- when the application starts (using spring.jpa.hibernate.ddl-auto=update)
-- 
-- The following tables will be created:
-- - teachers: Stores teacher information
-- - students: Stores student information  
-- - livestream_rooms: Stores livestream room information
-- - room_participants: Stores the mapping of students to rooms

-- Optional: Create a SQL Server login for the application
-- Uncomment and modify as needed

-- CREATE LOGIN livestream_user WITH PASSWORD = 'YourSecurePassword123!';
-- GO

-- CREATE USER livestream_user FOR LOGIN livestream_user;
-- GO

-- GRANT SELECT, INSERT, UPDATE, DELETE ON DATABASE::DBDACS_Livestream TO livestream_user;
-- GO

-- Verify database creation
SELECT 
    name as DatabaseName,
    create_date as CreatedDate,
    compatibility_level as CompatibilityLevel
FROM sys.databases 
WHERE name = 'DBDACS_Livestream';
GO

PRINT '';
PRINT '==================================================';
PRINT 'Database setup complete!';
PRINT 'Database Name: DBDACS_Livestream';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Update application.properties with your database connection details';
PRINT '2. Start the Spring Boot application';
PRINT '3. Spring Boot will automatically create the required tables';
PRINT '==================================================';
GO

-- Optional: View existing tables (run after starting the application)
-- SELECT TABLE_SCHEMA, TABLE_NAME 
-- FROM INFORMATION_SCHEMA.TABLES 
-- WHERE TABLE_TYPE = 'BASE TABLE'
-- ORDER BY TABLE_SCHEMA, TABLE_NAME;
-- GO
