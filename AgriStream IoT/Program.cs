using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AgriStream_IoT.Data;
using AgriStream_IoT.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Fetch the relational SQL connection string securely
string sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? throw new InvalidOperationException("Database connection string is missing from configuration.");

// 2. Register Azure SQL Server DB Context
builder.Services.AddDbContext<AgriDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

// 3. Register your Azure Storage Wrapper Service (Task c)
builder.Services.AddSingleton<IAgriStorageService, AgriStorageService>();

// 4. Register HttpClient for Azure Functions integration (Task d)
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Automatically connect to existing database infrastructure without deletion locks
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgriDbContext>();

    // Using simple creation confirmation checks ensures we attach to 'clam' without recreating it
    dbContext.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Route directly to user registration/login to begin verification testing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
