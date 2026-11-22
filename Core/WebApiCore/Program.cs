using System.Text;
using System.Threading.RateLimiting;
using ApplicationCore.Commands.Course;
using ApplicationCore.Commands.Progress;
using ApplicationCore.Commands.User;
using ApplicationCore.Commands.Video;
using ApplicationCore.Interfaces.Course;
using ApplicationCore.Interfaces.Progress;
using ApplicationCore.Interfaces.User;
using ApplicationCore.Interfaces.Video;
using ApplicationCore.Querys.Course;
using ApplicationCore.Querys.Progress;
using ApplicationCore.Querys.User;
using ApplicationCore.Querys.Video;
using ApplicationCore.RequestData.Course;
using ApplicationCore.RequestData.User;
using ApplicationCore.Validators.Course;
using ApplicationCore.Validators.User;
using DomainCore.RequestData.User;
using FluentValidation;
using InfrastructureCore.JWT;
using InfrastructureCore.LocalStorage;
using InfrastructureCore.ObjectStorage;
using InfrastructureCore.Persistence.Course;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.Progress;
using InfrastructureCore.Persistence.User;
using InfrastructureCore.Persistence.Video;
using InfrastructureCore.ProcessVideo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WebApiCore;
using WebApiCore.Controllers;
using WebApiCore.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SolutionVideos API",
        Version = "v1",
        Description = "API de la plataforma de cursos en video (.NET 8)"
    });
});

// ---------- Validación ----------
builder.Services.AddScoped<IValidator<UserData>, UserDataValidator>();
builder.Services.AddScoped<IValidator<UserLoginData>, UserLoginDataValidator>();
builder.Services.AddScoped<IValidator<CourseData>, CourseDataValidator>();

// ---------- Persistencia ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

// ---------- Opciones ----------
builder.Services.Configure<JwtOptions>(config.GetSection("Jwt"));
builder.Services.Configure<FfmepgOption>(config.GetSection("FFmpeg"));
builder.Services.Configure<ObjectStorageOptions>(config.GetSection("ObjectStorage"));

// ---------- Autenticación / autorización ----------
builder.Services.AddSingleton<IJwtService, JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config["Jwt:Issuer"],
        ValidAudience = config["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
});

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------- Llaves de Data Protection persistentes (Docker: volumen) ----------
var keysPath = config["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("SolutionVideos")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// ---------- Rate limiting: login y registro por IP ----------
var authPermit = config.GetValue("RateLimit:AuthPermitPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            """{"status":429,"message":"Demasiados intentos. Espera un minuto e inténtalo de nuevo."}""", ct);
    };
});

// ---------- CORS: solo orígenes configurados (en Docker todo es mismo origen vía nginx) ----------
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
    });
});

// ---------- Proxy inverso (nginx/Caddy): IP real del cliente ----------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---------- Casos de uso ----------
builder.Services.AddScoped<IUserCreate, UserCreate>();
builder.Services.AddScoped<IUserlogin, UserLogin>();
builder.Services.AddScoped<IUserValidatetoken, UserValidateToken>();
builder.Services.AddScoped<ICourseCreate, CourseCreate>();
builder.Services.AddScoped<ICourseByUserId, CourseByUserId>();
builder.Services.AddScoped<ICourseSearch, CourseSearch>();
builder.Services.AddScoped<ICourseGet, CourseGet>();
builder.Services.AddScoped<ICourseUpdate, CourseUpdate>();
builder.Services.AddScoped<ICourseDelete, CourseDelete>();
builder.Services.AddScoped<ICourseUpdateImage, CourseUpdateImage>();
builder.Services.AddScoped<ICourseStats, CourseStats>();
builder.Services.AddScoped<IUploadVideo, UploadVideo>();
builder.Services.AddScoped<IVideoByCourseId, VideoByCourseId>();
builder.Services.AddScoped<IVideosFromCourseId, VideosFromCourseId>();
builder.Services.AddScoped<IVideoDelete, VideoDelete>();
builder.Services.AddScoped<IVideoReprocess, VideoReprocess>();
builder.Services.AddScoped<IProgressByCourse, ProgressByCourse>();
builder.Services.AddScoped<IProgressSave, ProgressSave>();

// ---------- Repositorios e infraestructura ----------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IFfmpegService, FfmpegService>();
builder.Services.AddSingleton<IVideoQueue, VideoQueue>();

if (string.Equals(config["ObjectStorage:Provider"], "S3", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IObjectStorage, S3ObjectStorage>();
else
    builder.Services.AddSingleton<IObjectStorage, LocalObjectStorage>();

if (config.GetValue("VideoProcessing:Enabled", true))
    builder.Services.AddHostedService<VideoBackgroundService>();

var workRoot = config["FFmpeg:PathVideos"]!;
var storagePath = Path.Combine(workRoot, "storage");
var storagePathImages = config["Storage:BasePath"] ?? Path.Combine(workRoot, "images");
Directory.CreateDirectory(storagePath);
Directory.CreateDirectory(storagePathImages);

var app = builder.Build();

app.UseForwardedHeaders();

// En Docker la API vive detrás de nginx (HTTP interno): DisableHttpsRedirection=true
if (!app.Environment.IsDevelopment() && !config.GetValue<bool>("DisableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment() || config.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SolutionVideos API v1"));
}

var videoTypes = new FileExtensionContentTypeProvider();
videoTypes.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
videoTypes.Mappings[".ts"] = "video/mp2t";

app.UseRouting();
app.UseCors("CorsPolicy");

// Medios locales (proveedor Local). Los originales "source.*" nunca se sirven.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/storage") &&
        Path.GetFileName(ctx.Request.Path.Value ?? "").StartsWith("source.", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storagePath),
    RequestPath = "/storage",
    ContentTypeProvider = videoTypes,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl =
        ctx.File.Name.EndsWith(".m3u8") ? "public, max-age=60" : "public, max-age=31536000, immutable"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storagePathImages),
    RequestPath = "/images",
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await DatabaseInitializer.InitializeAsync(app.Services, config, app.Logger);

await app.RunAsync();

/// <summary>Expuesto para pruebas de integración (WebApplicationFactory).</summary>
public partial class Program { }
