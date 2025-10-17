
using MyApp.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.RTC;
using MyApp.Infrastructure.Services;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// SQL Server configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// above code registering the db context, And by default it is scoped 

// db context is design for short lived use , typically per HTTP request on per unit of work 
// if we use the sigleton, then multiple thread like multiple API calls will try to use the same same DB context as once

// also DB context tract the entities you load in the memory, 
// if we use the single ton then it will accumulate the huge memory over a time 


builder.Services.AddIdentity<ApplicationUser, IdentityRole>() // here 
                                                              // Indentity user is the build in class, which is for user and admin 
    .AddEntityFrameworkStores<AppDbContext>() // tell that, stored the user and role in your data base 
    .AddDefaultTokenProviders(); // this is basically token provider (password reset token, email confirmation token )

var jwtSettings = builder.Configuration.GetSection("JWT");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // sceme for autentication
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // sceme for chech authenticate 
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Check if the request has the JWT cookie
            if (context.Request.Cookies.ContainsKey("jwtToken"))
            {
                context.Token = context.Request.Cookies["jwtToken"];

            }
            return Task.CompletedTask;
        }
    };

    // this is just the token validation parameter 
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, // THIS ensures expired JWTs are rejected
        ClockSkew = TimeSpan.FromSeconds(0), // no extra time, token expires exactly
        ValidIssuer = jwtSettings["ValidIssuer"],
        ValidAudience = jwtSettings["ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]))
    };
});

// add authorication in app 
builder.Services.AddAuthorization();

builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();


// this is also has be scoped 
// because it uses the db context which is scoped 
//if we use this as singleton, it will expect the singleton db context 
// then it will give the runtime error 

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173")   // Allow requests from any domain
              .AllowAnyMethod()   // Allow GET, POST, PUT, DELETE
              .AllowAnyHeader()
              .AllowCredentials();  // Allow headers like Content-Type, Authorization
    });
});

builder.Services.AddSignalR();

var app = builder.Build();
// Use CORS
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<NotificationHub>("/notificationHub");
});

using (var scope = app.Services.CreateScope())
{
    // adding the roles here in RoleManager 
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "User", "SuperAdmin" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
}
app.Run();
