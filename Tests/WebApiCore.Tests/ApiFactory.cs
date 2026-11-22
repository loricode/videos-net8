using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainCore.Constants;
using DomainCore.DTOs.User;
using InfrastructureCore.Persistence.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiCore.Tests;

/// <summary>
/// API real en memoria con SQLite. Sin FFmpeg (VideoProcessing deshabilitado) y con
/// carpeta de medios temporal por instancia.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public string MediaRoot { get; } = Path.Combine(Path.GetTempPath(), "sv-tests", Guid.NewGuid().ToString("N"));
    public int AuthPermitPerMinute { get; init; } = 1000;

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("FFmpeg:PathVideos", MediaRoot);
        builder.UseSetting("FFmpeg:PathOrigen", "ffmpeg-no-disponible");
        builder.UseSetting("Storage:BasePath", Path.Combine(MediaRoot, "images"));
        builder.UseSetting("Database:AutoMigrate", "false");
        builder.UseSetting("Database:EnsureCreated", "true");
        builder.UseSetting("VideoProcessing:Enabled", "false");
        builder.UseSetting("DisableHttpsRedirection", "true");
        builder.UseSetting("RateLimit:AuthPermitPerMinute", AuthPermitPerMinute.ToString());
        builder.UseSetting("Seed:AdminEmail", "");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
        try { if (Directory.Exists(MediaRoot)) Directory.Delete(MediaRoot, true); } catch { /* temporal */ }
    }

    // ---------- helpers ----------

    public async Task<(HttpClient Client, Guid UserId)> CreateUserClientAsync(string role, string? email = null)
    {
        email ??= $"{role.ToLower()}-{Guid.NewGuid():N}@test.local";
        const string password = "Clave123!";

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Email = email, UserName = email, FullName = role };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, role);
        }

        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/User/Login", new { email, password });
        login.EnsureSuccessStatusCode();
        var dto = (await login.Content.ReadFromJsonAsync<UserDetailDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.Token);
        return (client, dto.UserId);
    }

    public Task<(HttpClient Client, Guid UserId)> InstructorAsync() => CreateUserClientAsync(Roles.Instructor);
    public Task<(HttpClient Client, Guid UserId)> StudentAsync() => CreateUserClientAsync(Roles.Student);

    public static MultipartFormDataContent CourseForm(string title, bool state = true, string fileName = "cover.png")
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(title), "title" },
            { new StringContent("Descripción de prueba"), "description" },
            { new StringContent(state ? "true" : "false"), "state" },
        };
        var file = new ByteArrayContent(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);
        return form;
    }

    public static MultipartFormDataContent VideoForm(Guid courseId, string fileName = "clase.mp4")
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("Lección"), "title" },
            { new StringContent("Intro"), "description" },
            { new StringContent(courseId.ToString()), "courseId" },
        };
        var file = new ByteArrayContent(new byte[2048]);
        file.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        form.Add(file, "file", fileName);
        return form;
    }

    public async Task<Guid> CreateCourseAsync(HttpClient client, string title = "Curso", bool state = true)
    {
        var res = await client.PostAsync("/Course/Create", CourseForm(title, state));
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>Marca un video como listo (simula el trabajo de FFmpeg).</summary>
    public async Task MarkReadyAsync(Guid videoId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var v = await db.Videos.FindAsync(videoId);
        v!.Status = VideoStatus.Ready;
        v.StreamUrl = $"/storage/{v.CourseId}/{v.Id}/playlist.m3u8";
        await db.SaveChangesAsync();
    }

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
    }
}
