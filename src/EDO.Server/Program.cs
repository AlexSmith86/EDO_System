using System.Text;
using EDO.Server.Data;
using EDO.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// === Лимиты загрузки файлов — 50 МБ ===
// Должно совпадать с LocalFileStorageService.MaxFileSizeBytes.
// Kestrel по умолчанию режет тело запроса на ~30 МБ; для приёма вложений
// поднимаем до 50 МБ и синхронизируем FormOptions, чтобы multipart
// тоже не упирался в дефолтный лимит.
const long MaxUploadBytes = 50L * 1024 * 1024;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadBytes;
    options.ValueLengthLimit = int.MaxValue;
});

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var usePostgres = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePostgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelegramBotService>());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Гарантируем, что папка wwwroot/uploads/attachments существует до старта:
// app.UseStaticFiles() требует наличия wwwroot, иначе вложения не будут
// отдаваться по /uploads/attachments/*.
var webRootPath = string.IsNullOrWhiteSpace(app.Environment.WebRootPath)
    ? Path.Combine(app.Environment.ContentRootPath, "wwwroot")
    : app.Environment.WebRootPath;
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "attachments"));

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EDO.Server.Data.AppDbContext>();
    if (usePostgres)
    {
        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex) when (
            ex.GetType().Name == "PostgresException" ||
            ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            context.Database.EnsureDeleted();
            context.Database.Migrate();
        }
    }
    else
    {
        context.Database.EnsureCreated();
    }

    var userRole = context.Roles.FirstOrDefault(r => r.Name == "Пользователь");
    if (userRole == null)
    {
        context.Roles.Add(new EDO.Server.Models.Role
        {
            Name = "Пользователь",
            Description = "Базовый пользователь системы"
        });
        context.SaveChanges();
    }

    var admin = context.Users.FirstOrDefault(u => u.Email == "admin@growtech.com");
    if (admin == null)
    {
        var role = context.Roles.FirstOrDefault(r => r.Name == "Администратор");
        if (role == null)
        {
            role = new EDO.Server.Models.Role { Name = "Администратор", Description = "Системный администратор" };
            context.Roles.Add(role);
            context.SaveChanges();
        }

        context.Users.Add(new EDO.Server.Models.User
        {
            FirstName = "Александр",
            LastName = "Админ",
            Position = "Директор",
            Email = "admin@growtech.com",
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        context.SaveChanges();
    }
    else if (!BCrypt.Net.BCrypt.Verify("123456", admin.PasswordHash))
    {
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
        context.SaveChanges();
    }

    SeedData.SeedCategories(context);
    SeedData.SeedApprovalStages(context);
    SeedData.SeedEmployees(context);
}

app.UseCors();

// Serve Blazor WASM client static files (for production single-container deployment)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Blazor WASM SPA fallback: any non-API route serves index.html
app.MapFallbackToFile("index.html");

app.Run();
