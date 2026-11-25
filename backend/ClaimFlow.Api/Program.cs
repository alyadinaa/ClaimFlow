using ClaimFlow.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ClaimFlow")
    ?? throw new InvalidOperationException("Connection string 'ClaimFlow' is not configured.");

var sqlite = new SqliteConnectionStringBuilder(connectionString);
if (sqlite.DataSource != ":memory:" && !Path.IsPathRooted(sqlite.DataSource))
{
    sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
}

builder.Services.AddDbContext<ClaimFlowDbContext>(options =>
    options.UseSqlite(sqlite.ConnectionString));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
