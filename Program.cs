using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using StudentManagementAPI.Data;
using StudentManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Enable CORS so client applications can call the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Management API",
        Version = "v1",
        Description = "RESTful API for managing Student records using ASP.NET Core and ADO.NET (Microsoft.Data.SqlClient)"
    });

    // Provide default example values in Swagger UI so clicking 'Execute' works immediately without validation errors
    options.MapType<Student>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["id"] = new OpenApiSchema
            {
                Type = "integer",
                Example = new OpenApiInteger(0),
                Description = "ID (leave 0 or omit when creating a student)"
            },
            ["name"] = new OpenApiSchema
            {
                Type = "string",
                Example = new OpenApiString("Daniel Miller")
            },
            ["email"] = new OpenApiSchema
            {
                Type = "string",
                Example = new OpenApiString("daniel.miller@example.com")
            },
            ["course"] = new OpenApiSchema
            {
                Type = "string",
                Example = new OpenApiString("Cyber Security")
            }
        },
        Required = new HashSet<string> { "name", "email", "course" }
    });
});

var app = builder.Build();

// Automatically verify/create database and tables on startup (with candidate instance auto-discovery)
await DatabaseInitializer.InitializeAsync(app.Configuration, app.Logger);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Management API v1");
    });
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
