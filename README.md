# Student Management API (.NET 8 Web API & ADO.NET)

A RESTful Web API for managing student records built using **ASP.NET Core (.NET 8)**, **ADO.NET (`Microsoft.Data.SqlClient`)**, and **SQL Server / LocalDB**.

---

## 🛠️ Issues Identified & Fixed

1. **SQL Connection SSL Certificate Failure (`TrustServerCertificate`)**:
   - *Issue*: `Microsoft.Data.SqlClient` v5+ enables encryption by default (`Encrypt=True`). When connecting to local SQL Server / `(localdb)\MSSQLLocalDB` without a trusted SSL certificate, requests immediately threw an untrusted certificate exception.
   - *Fix*: Added `TrustServerCertificate=True;` to the connection string in `appsettings.json`.

2. **Missing Database & Table Automation**:
   - *Issue*: Running the project on a fresh machine caused SQL errors (`Cannot open database 'StudentAPI'` / `Invalid object name 'Students'`) because the database and table did not exist.
   - *Fix*: Created [`DatabaseInitializer.cs`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/Data/DatabaseInitializer.cs) to automatically create the database, table, and sample records upon application startup. Also provided [`SetupDatabase.sql`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/SetupDatabase.sql) for manual setup if preferred.

3. **Synchronous Database Operations Blocking the Thread Pool**:
   - *Issue*: The controller was using synchronous calls (`con.Open()`, `cmd.ExecuteReader()`, `cmd.ExecuteNonQuery()`), which is an anti-pattern in ASP.NET Core that causes thread starvation under load.
   - *Fix*: Refactored all controller actions to fully asynchronous `async/await` (`OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`, `await using`).

4. **Missing Model Validation**:
   - *Issue*: Client could submit empty strings, null values, or invalid emails without validation, causing DB failures or corrupt data.
   - *Fix*: Added data annotations (`[Required]`, `[EmailAddress]`, `[StringLength]`) to [`Student.cs`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/Models/Student.cs) and validation checks in the controller.

5. **Null Safety & Parameter Handling in ADO.NET**:
   - *Issue*: Passing a `null` string to `cmd.Parameters.AddWithValue` in ADO.NET throws an argument exception instead of inserting SQL `NULL`. Also reading `reader["Column"].ToString()!` on null values can cause issues.
   - *Fix*: Used `(object?)student.Field ?? DBNull.Value` for parameters and `reader.IsDBNull(...)` when reading records.

6. **Missing Error Handling**:
   - *Issue*: Raw database exceptions would propagate as 500 server crashes leaking internal details.
   - *Fix*: Added comprehensive `try-catch` blocks catching `SqlException` and general `Exception` with clear error messages and logging.

7. **Outdated Test Requests**:
   - *Issue*: `StudentManagementAPI.http` only contained a default weatherforecast test.
   - *Fix*: Replaced [`StudentManagementAPI.http`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/StudentManagementAPI.http) with a complete set of CRUD test requests and validation test cases.

8. **CORS Policy Added**:
   - *Fix*: Enabled CORS in `Program.cs` so web frontends or external clients can interact with the API without cross-origin blocks.

---

## 🚀 How to Run the Project

### Option A: Using Visual Studio
1. Open [`StudentManagementAPI.sln`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/StudentManagementAPI.sln) in Visual Studio 2022.
2. Press **F5** or click **Run (https / http)**.
3. The browser will open automatically at `https://localhost:7088/swagger` (or `http://localhost:5215/swagger`).

### Option B: Using .NET CLI
In terminal:
```bash
cd "c:\Users\konat\Downloads\P10 - StudentManagementAPI\P10 - StudentManagementAPI\StudentManagementAPI"
dotnet run
```
Then navigate to: [http://localhost:5215/swagger](http://localhost:5215/swagger)

---

## 🗄️ Database Setup

The API automatically creates the `StudentAPI` database and `Students` table on startup!

If you prefer to create the database manually using **SQL Server Management Studio (SSMS)**:
1. Open SSMS and connect to `(localdb)\MSSQLLocalDB` or your local SQL Server instance.
2. Open [`SetupDatabase.sql`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/SetupDatabase.sql).
3. Click **Execute**.

---

## 📡 API Endpoints & Expected Output

### 1. Get All Students
- **Method / Route**: `GET /api/students`
- **Status Code**: `200 OK`
- **Expected Output**:
```json
[
  {
    "id": 1,
    "name": "Alice Johnson",
    "email": "alice.johnson@example.com",
    "course": "Computer Science"
  },
  {
    "id": 2,
    "name": "Bob Smith",
    "email": "bob.smith@example.com",
    "course": "Information Technology"
  },
  {
    "id": 3,
    "name": "Charlie Brown",
    "email": "charlie.brown@example.com",
    "course": "Data Science"
  }
]
```

---

### 2. Get Student by ID
- **Method / Route**: `GET /api/students/{id}` (e.g. `/api/students/1`)
- **Status Code**: `200 OK`
- **Expected Output**:
```json
{
  "id": 1,
  "name": "Alice Johnson",
  "email": "alice.johnson@example.com",
  "course": "Computer Science"
}
```
- **If Not Found (`GET /api/students/999`)**:
  - **Status Code**: `404 Not Found`
  - **Output**:
  ```json
  {
    "message": "Student with ID 999 was not found."
  }
  ```

---

### 3. Add a New Student
- **Method / Route**: `POST /api/students`
- **Headers**: `Content-Type: application/json`
- **Request Body**:
```json
{
  "name": "Daniel Miller",
  "email": "daniel.miller@example.com",
  "course": "Cyber Security"
}
```
- **Status Code**: `201 Created`
- **Response Headers**: `Location: /api/Students/4`
- **Expected Output**:
```json
{
  "id": 4,
  "name": "Daniel Miller",
  "email": "daniel.miller@example.com",
  "course": "Cyber Security"
}
```

---

### 4. Update an Existing Student
- **Method / Route**: `PUT /api/students/{id}` (e.g. `/api/students/1`)
- **Headers**: `Content-Type: application/json`
- **Request Body**:
```json
{
  "id": 1,
  "name": "Alice Johnson Updated",
  "email": "alice.updated@example.com",
  "course": "Artificial Intelligence"
}
```
- **Status Code**: `200 OK`
- **Expected Output**:
```json
{
  "message": "Student updated successfully.",
  "student": {
    "id": 1,
    "name": "Alice Johnson Updated",
    "email": "alice.updated@example.com",
    "course": "Artificial Intelligence"
  }
}
```

---

### 5. Delete a Student
- **Method / Route**: `DELETE /api/students/{id}` (e.g. `/api/students/3`)
- **Status Code**: `200 OK`
- **Expected Output**:
```json
{
  "message": "Student with ID 3 deleted successfully."
}
```
- **If ID does not exist**:
  - **Status Code**: `404 Not Found`
  - **Output**:
  ```json
  {
    "message": "Student with ID 999 was not found."
  }
  ```

---

### 6. Validation Error Example
- **Method / Route**: `POST /api/students`
- **Request Body**:
```json
{
  "name": "",
  "email": "invalid-email-string",
  "course": ""
}
```
- **Status Code**: `400 Bad Request`
- **Expected Output**:
```json
{
  "errors": {
    "Name": ["Name is required."],
    "Email": ["Invalid email format."],
    "Course": ["Course is required."]
  }
}
```

---

## 🧪 How to Test the Output

You have multiple ways to test:

1. **Swagger UI (Interactive Browser Testing)**:
   - Run the app and visit: `http://localhost:5215/swagger` or `https://localhost:7088/swagger`.
   - Click on any endpoint (e.g., `GET /api/Students`, `POST /api/Students`).
   - Click **Try it out**, fill in parameters or payload, and click **Execute**.

2. **Visual Studio / VS Code HTTP File**:
   - Open [`StudentManagementAPI.http`](file:///c:/Users/konat/Downloads/P10%20-%20StudentManagementAPI/P10%20-%20StudentManagementAPI/StudentManagementAPI/StudentManagementAPI.http).
   - Click **Send Request** above any test case.

3. **cURL (Command Line)**:
   ```bash
   # Get all students
   curl -X GET "http://localhost:5215/api/students"

   # Add a student
   curl -X POST "http://localhost:5215/api/students" -H "Content-Type: application/json" -d "{\"name\":\"Eva Green\",\"email\":\"eva@example.com\",\"course\":\"Robotics\"}"

   # Get student by ID
   curl -X GET "http://localhost:5215/api/students/1"

   # Update student
   curl -X PUT "http://localhost:5215/api/students/1" -H "Content-Type: application/json" -d "{\"name\":\"Eva White\",\"email\":\"eva.white@example.com\",\"course\":\"Robotics\"}"

   # Delete student
   curl -X DELETE "http://localhost:5215/api/students/1"
   ```
