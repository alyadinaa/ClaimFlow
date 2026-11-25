using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClaimFlow.Tests;

// Runs the real application in memory, with an in-memory database that lives as long as the factory.
public sealed class ApiFactory(string environment = "Development", string? jwtKey = null) : WebApplicationFactory<Program>
{
    public const string ClaimantEmail = "claimant@demo.example";
    public const string ClaimantPassword = "Demo-Claimant-1";
    public const string OfficerEmail = "officer@demo.example";
    public const string OfficerPassword = "Demo-Officer-1";

    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment(environment);
        // A second safeguard: tests can never open the development database file.
        builder.UseSetting("ConnectionStrings:ClaimFlow", "Data Source=:memory:");
        if (jwtKey is not null)
        {
            builder.UseSetting("Jwt:Key", jwtKey);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ClaimFlowDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ClaimFlowDbContext>>();
            services.AddDbContext<ClaimFlowDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public T WithDb<T>(Func<ClaimFlowDbContext, T> action)
    {
        using var scope = Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<ClaimFlowDbContext>());
    }

    public void AddUser(string email, string password, UserRole role)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimFlowDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        db.Users.Add(new User(email, email, hasher.HashPassword(null!, password), role));
        db.SaveChanges();
    }

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    public Task<HttpClient> LoginAsClaimantAsync() => LoginAsync(ClaimantEmail, ClaimantPassword);

    public Task<HttpClient> LoginAsOfficerAsync() => LoginAsync(OfficerEmail, OfficerPassword);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
