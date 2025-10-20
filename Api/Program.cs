using System.Text;
using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();

// JWT
var key = builder.Configuration["Jwt:Key"] ?? throw new Exception("Missing Jwt:Key");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = false, ValidateAudience = false,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("ng", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors("ng");
app.UseSwagger(); app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// DB init + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var pwd = scope.ServiceProvider.GetRequiredService<PasswordService>();
    await Seed.RunAsync(db, (string s) => {
        var (h, k) = pwd.CreateHash(s);
        return (h, k);
    });
}

app.Run();
