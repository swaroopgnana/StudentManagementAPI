using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentManagementAPI.Data;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StudentsController> _logger;

        // In-memory fallback cache so Swagger endpoints work seamlessly even if SQL Server is not running
        private static readonly List<Student> InMemoryStudents = new()
        {
            new Student { Id = 1, Name = "Alice Johnson", Email = "alice.johnson@example.com", Course = "Computer Science" },
            new Student { Id = 2, Name = "Bob Smith", Email = "bob.smith@example.com", Course = "Information Technology" },
            new Student { Id = 3, Name = "Charlie Brown", Email = "charlie.brown@example.com", Course = "Data Science" }
        };
        private static int _nextInMemoryId = 4;
        private static readonly object _syncLock = new();

        public StudentsController(IConfiguration configuration, ILogger<StudentsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<SqlConnection?> TryGetOpenConnectionAsync()
        {
            string? connStr = DatabaseInitializer.WorkingConnectionString 
                              ?? _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connStr))
            {
                return null;
            }

            try
            {
                var con = new SqlConnection(connStr);
                await con.OpenAsync();
                return con;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not open SQL Server connection. Using in-memory fallback.");
                return null;
            }
        }

        // GET: api/students
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Student>))]
        public async Task<IActionResult> GetStudents()
        {
            SqlConnection? con = null;
            try
            {
                con = await TryGetOpenConnectionAsync();
                if (con != null)
                {
                    var students = new List<Student>();
                    const string query = "SELECT Id, Name, Email, Course FROM Students";

                    await using var cmd = new SqlCommand(query, con);
                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        students.Add(new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                            Course = reader.IsDBNull(reader.GetOrdinal("Course")) ? string.Empty : reader.GetString(reader.GetOrdinal("Course"))
                        });
                    }

                    Response.Headers["X-Data-Source"] = "SQL Server";
                    return Ok(students);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database read failed; serving from in-memory fallback.");
            }
            finally
            {
                if (con != null) await con.DisposeAsync();
            }

            // In-Memory Fallback
            Response.Headers["X-Data-Source"] = "In-Memory Fallback (SQL Server not running)";
            lock (_syncLock)
            {
                return Ok(InMemoryStudents.ToList());
            }
        }

        // GET: api/students/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Student))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudent(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Please enter a valid student ID greater than 0 (e.g. 1)." });
            }

            SqlConnection? con = null;
            try
            {
                con = await TryGetOpenConnectionAsync();
                if (con != null)
                {
                    const string query = "SELECT Id, Name, Email, Course FROM Students WHERE Id = @Id";
                    await using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id", id);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                            Course = reader.IsDBNull(reader.GetOrdinal("Course")) ? string.Empty : reader.GetString(reader.GetOrdinal("Course"))
                        };

                        Response.Headers["X-Data-Source"] = "SQL Server";
                        return Ok(student);
                    }

                    return NotFound(new { message = $"Student with ID {id} was not found." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed for ID {Id}; checking in-memory fallback.", id);
            }
            finally
            {
                if (con != null) await con.DisposeAsync();
            }

            // In-Memory Fallback
            Response.Headers["X-Data-Source"] = "In-Memory Fallback (SQL Server not running)";
            lock (_syncLock)
            {
                var student = InMemoryStudents.FirstOrDefault(s => s.Id == id);
                if (student != null)
                {
                    return Ok(student);
                }
                return NotFound(new { message = $"Student with ID {id} was not found." });
            }
        }

        // POST: api/students
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Student))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddStudent([FromBody] Student student)
        {
            if (student == null)
            {
                return BadRequest(new { message = "Student data is required." });
            }

            if (string.IsNullOrWhiteSpace(student.Name))
            {
                student.Name = "New Student";
            }
            if (string.IsNullOrWhiteSpace(student.Email))
            {
                student.Email = "student@example.com";
            }
            if (string.IsNullOrWhiteSpace(student.Course))
            {
                student.Course = "General Studies";
            }

            SqlConnection? con = null;
            try
            {
                con = await TryGetOpenConnectionAsync();
                if (con != null)
                {
                    const string query = @"
                        INSERT INTO Students (Name, Email, Course)
                        VALUES (@Name, @Email, @Course);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    await using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Name", (object?)student.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)student.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Course", (object?)student.Course ?? DBNull.Value);

                    var insertedId = await cmd.ExecuteScalarAsync();
                    if (insertedId != null && insertedId != DBNull.Value)
                    {
                        student.Id = Convert.ToInt32(insertedId);
                    }

                    Response.Headers["X-Data-Source"] = "SQL Server";
                    return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database insert failed; falling back to in-memory store.");
            }
            finally
            {
                if (con != null) await con.DisposeAsync();
            }

            // In-Memory Fallback
            Response.Headers["X-Data-Source"] = "In-Memory Fallback (SQL Server not running)";
            lock (_syncLock)
            {
                student.Id = _nextInMemoryId++;
                InMemoryStudents.Add(new Student
                {
                    Id = student.Id,
                    Name = student.Name,
                    Email = student.Email,
                    Course = student.Course
                });

                return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
            }
        }

        // PUT: api/students/1
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] Student student)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Please enter a valid student ID greater than 0 (e.g. 1)." });
            }

            if (student == null)
            {
                return BadRequest(new { message = "Student data is required." });
            }

            if (string.IsNullOrWhiteSpace(student.Name))
            {
                student.Name = "Updated Student";
            }
            if (string.IsNullOrWhiteSpace(student.Email))
            {
                student.Email = "updated@example.com";
            }
            if (string.IsNullOrWhiteSpace(student.Course))
            {
                student.Course = "General Studies";
            }

            student.Id = id;

            SqlConnection? con = null;
            try
            {
                con = await TryGetOpenConnectionAsync();
                if (con != null)
                {
                    const string query = @"
                        UPDATE Students
                        SET Name = @Name,
                            Email = @Email,
                            Course = @Course
                        WHERE Id = @Id";

                    await using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", (object?)student.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)student.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Course", (object?)student.Course ?? DBNull.Value);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = $"Student with ID {id} was not found." });
                    }

                    Response.Headers["X-Data-Source"] = "SQL Server";
                    return Ok(new
                    {
                        message = "Student updated successfully.",
                        student
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database update failed; updating in-memory store.");
            }
            finally
            {
                if (con != null) await con.DisposeAsync();
            }

            // In-Memory Fallback
            Response.Headers["X-Data-Source"] = "In-Memory Fallback (SQL Server not running)";
            lock (_syncLock)
            {
                var existing = InMemoryStudents.FirstOrDefault(s => s.Id == id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Student with ID {id} was not found." });
                }

                existing.Name = student.Name;
                existing.Email = student.Email;
                existing.Course = student.Course;

                return Ok(new
                {
                    message = "Student updated successfully.",
                    student = existing
                });
            }
        }

        // DELETE: api/students/1
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Please enter a valid student ID greater than 0 (e.g. 1)." });
            }

            SqlConnection? con = null;
            try
            {
                con = await TryGetOpenConnectionAsync();
                if (con != null)
                {
                    const string query = "DELETE FROM Students WHERE Id = @Id";
                    await using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id", id);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = $"Student with ID {id} was not found." });
                    }

                    Response.Headers["X-Data-Source"] = "SQL Server";
                    return Ok(new { message = $"Student with ID {id} deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database delete failed; checking in-memory store.");
            }
            finally
            {
                if (con != null) await con.DisposeAsync();
            }

            // In-Memory Fallback
            Response.Headers["X-Data-Source"] = "In-Memory Fallback (SQL Server not running)";
            lock (_syncLock)
            {
                var existing = InMemoryStudents.FirstOrDefault(s => s.Id == id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Student with ID {id} was not found." });
                }

                InMemoryStudents.Remove(existing);
                return Ok(new { message = $"Student with ID {id} deleted successfully." });
            }
        }
    }
}
