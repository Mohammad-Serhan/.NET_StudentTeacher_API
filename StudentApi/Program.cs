using Auth.Shared.Controllers;
using Auth.Shared.MiddleWare;
using Auth.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using StudentApi.Middleware;
using StudentApi.Services;
using System.Text;
using System.Threading.RateLimiting;




var builder = WebApplication.CreateBuilder(args);


// Add services to the container
builder.Services.AddControllers();


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();


// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global fallback policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            AutoReplenishment = true
        });
    });

    // Named policies for different endpoints
    options.AddFixedWindowLimiter("ModerateApiPolicy", policyOptions =>
    {
        policyOptions.PermitLimit = 100;
        policyOptions.Window = TimeSpan.FromMinutes(1);
        policyOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policyOptions.QueueLimit = 5;
        policyOptions.AutoReplenishment = true;
    });

    // Custom rejection response
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter =
            ((context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter : TimeSpan.FromSeconds(60)).TotalSeconds).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            message = "Please try again later.",
            retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? retry.TotalSeconds : 60
        }, token);
    };
});




// CSRF Protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.None; // Allow the CSRF cookie to be sent with cross-site requests
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure the cookie is sent over HTTPS
});


// Application Services
builder.Services.AddSingleton<ISimpleEncryptionService, SimpleEncryptionService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<ITokenService, JwtService>();
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
var jwtSecretKey = builder.Configuration["JwtSettings:Key"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer is not configured");
var jwtAudience = builder.Configuration["JwtSettings:Audience"]
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
//app.UseMiddleware<GlobalExceptionMiddleware>();
//app.UseMiddleware<ClientInfoMiddleware>();




// Security Middleware
app.UseAuthentication();                             // ← First: "Who are you?"
// HttpContext.User is now populated with claims
// JWT is decoded, user identity established
app.UseRateLimiter(); // Apply Rate Limiting early

app.UseAuthorization();
app.UseAntiforgery(); // Place Antiforgery middleware before MapControllers


// 
app.UseMiddleware<Jwt_Check_Middleware>();


app.MapControllers();

app.Run();