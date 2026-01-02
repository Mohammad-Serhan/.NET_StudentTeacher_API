using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StudentApi.Middleware;
using StudentApi.Services;
using System.Text;
using System.Threading.RateLimiting;
using IConfigService = Auth.Shared.Services.IConfigService;
using IJwsService = Auth.Shared.Services.IJwsService;
using ISimpleEncryptionService = Auth.Shared.Services.ISimpleEncryptionService;

using Auth.Shared.Contracts;
using Auth.Shared.Services;
using SimpleEncryptionService = Auth.Shared.Services.SimpleEncryptionService;
using ConfigService = Auth.Shared.Services.ConfigService;
using JwsService = Auth.Shared.Services.JwsService;



var builder = WebApplication.CreateBuilder(args);





// Add services to the container
builder.Services.AddControllers();




builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();



// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    var rateLimitOptions = new FixedWindowRateLimiterOptions
    {
        AutoReplenishment = true,
        PermitLimit = 150,
        Window = TimeSpan.FromMinutes(1)
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => rateLimitOptions);
    });


    options.AddPolicy("ModerateApiPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 150,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

// CSRF Protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.None; // Allow the CSRF cookie to be sent with cross-site requests
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure the cookie is sent over HTTPS
});

// Application Services
builder.Services.AddSingleton<ISimpleEncryptionService, SimpleEncryptionService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IJwsService, JwsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IExportService, ExportService>();


// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000"
            )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer is not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();



app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowReactApp");


// Custom Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ClientInfoMiddleware>();

// Security Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Apply Rate Limiting early
app.UseAntiforgery(); // Place Antiforgery middleware before MapControllers


// Dynamic Authorization (if needed)
app.UseMiddleware<DynamicAuthorizationMiddleware>();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    await next();
});



app.MapControllers();

app.Run();