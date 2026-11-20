-- =============================================
-- StudentManagementAPI Database Setup Script
-- Database: StudentAPI
-- Table: Students
-- =============================================

-- 1. Create Database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'StudentAPI')
BEGIN
    CREATE DATABASE [StudentAPI];
    PRINT 'Database [StudentAPI] created successfully.';
END
ELSE
BEGIN
    PRINT 'Database [StudentAPI] already exists.';
END
GO

-- 2. Switch to StudentAPI context
USE [StudentAPI];
GO

-- 3. Create Students table if it does not exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Students')
BEGIN
    CREATE TABLE Students (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        Course NVARCHAR(100) NOT NULL
    );
    PRINT 'Table [Students] created successfully.';

    -- 4. Insert Seed Sample Data
    INSERT INTO Students (Name, Email, Course) VALUES
    ('Alice Johnson', 'alice.johnson@example.com', 'Computer Science'),
    ('Bob Smith', 'bob.smith@example.com', 'Information Technology'),
    ('Charlie Brown', 'charlie.brown@example.com', 'Data Science');

    PRINT 'Sample student records inserted.';
END
ELSE
BEGIN
    PRINT 'Table [Students] already exists.';
END
GO

-- 5. Verification Query
SELECT * FROM Students;
GO
