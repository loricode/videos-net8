using ApplicationCore.Commands.Course;
using ApplicationCore.Commands.User;
using ApplicationCore.Commands.Video;
using ApplicationCore.Interfaces.Course;
using ApplicationCore.Interfaces.User;
using ApplicationCore.Interfaces.Video;
using ApplicationCore.Querys.Course;
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
using InfrastructureCore.Persistence.Course;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.User;
using InfrastructureCore.Persistence.Video;
using InfrastructureCore.ProcessVideo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using WebApiCore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mi API",
        Version = "v1",
        Description = "API desarrollada en .NET 8"
    });
});

builder.Services.AddScoped<IValidator<UserData>, UserDataValidator>();
builder.Services.AddScoped<IValidator<UserLoginData>, UserLoginDataValidator>();
builder.Services.AddScoped<IValidator<CourseData>, CourseDataValidator>();

builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IJwtService, JwtService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<FfmepgOption>(builder.Configuration.GetSection("FFmpeg"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
});

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IUserCreate, UserCreate>();
builder.Services.AddScoped<IUserlogin, UserLogin>();
builder.Services.AddScoped<ICourseCreate, CourseCreate>();
builder.Services.AddScoped<IUploadVideo, UploadVideo>();
builder.Services.AddScoped<IUserValidatetoken, UserValidateToken>();
builder.Services.AddScoped<ICourseByUserId, CourseByUserId>();
builder.Services.AddScoped<IVideoByCourseId, VideoByCourseId>();
builder.Services.AddScoped<ICourseSearch, CourseSearch>();
builder.Services.AddScoped<IVideosFromCourseId, VideosFromCourseId>();
builder.Services.AddScoped<ICourseGet, CourseGet>();
builder.Services.AddScoped<ICourseUpdate, CourseUpdate>();
builder.Services.AddScoped<ICourseDelete, CourseDelete>();




builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();

builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IFfmpegService, FfmpegService>();
builder.Services.AddSingleton<IVideoQueue, VideoQueue>();

builder.Services.AddHostedService<VideoBackgroundService>();

var storagePathImages = builder.Configuration["Storage:BasePath"]!;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API V1");
});

var storagePath = @"C:\videos-app\storage";

var provider = new FileExtensionContentTypeProvider();

provider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";

provider.Mappings[".ts"] = "video/mp2t";

var imageProvider = new FileExtensionContentTypeProvider();
imageProvider.Mappings[".jpg"] = "image/jpeg";
imageProvider.Mappings[".jpeg"] = "image/jpeg";
imageProvider.Mappings[".png"] = "image/png";
imageProvider.Mappings[".webp"] = "image/webp";
imageProvider.Mappings[".gif"] = "image/gif";

app.UseRouting();

app.UseCors("CorsPolicy");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storagePath),
    RequestPath = "/storage",
    ContentTypeProvider = provider
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storagePathImages),
    RequestPath = "/images",
    ContentTypeProvider = imageProvider
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseMiddleware<RequestTimingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
