using System.Text;
using EDO.Server.Data;
using EDO.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EDO.Server.Data.AppDbContext>();
    context.Database.Migrate();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
