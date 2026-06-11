using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using whstore.Data;
using whstore.Services;
using System;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ১. ডাটাবেস সার্ভিস রেজিস্টার
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        options.UseNpgsql(BuildPostgresConnectionString(databaseUrl));
    }
    else
    {
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
    }
});

static string BuildPostgresConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    if (uri.Scheme != "postgres" && uri.Scheme != "postgresql")
    {
        throw new InvalidOperationException("DATABASE_URL must use postgres:// or postgresql:// scheme.");
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = SslMode.Require
    };

    if (!string.IsNullOrEmpty(uri.Query))
    {
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var kv in query)
        {
            var parts = kv.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].ToLowerInvariant();
            var value = Uri.UnescapeDataString(parts[1]);
            if (key == "sslmode" && Enum.TryParse<SslMode>(value, true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }
        }
    }

    return builder.ConnectionString;
}

// ২. ফাইল আপলোডের লিমিট
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
    options.MemoryBufferThreshold = 1 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();

// ৩. গুগল ড্রাইভ সার্ভিস রেজিস্টার
builder.Services.AddScoped<GoogleDriveService>();

// ৪. অথেন্টিকেশন
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/Error";
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS embeds (
            id INTEGER NOT NULL CONSTRAINT PK_embeds PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            embedurl TEXT NOT NULL,
            embedtype TEXT NULL,
            description TEXT NULL,
            isvisible INTEGER NOT NULL,
            createdat TEXT NOT NULL
        );");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS products (
            id INTEGER NOT NULL CONSTRAINT PK_products PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            category TEXT NULL,
            price TEXT NOT NULL,
            originalprice TEXT NULL,
            imageurl TEXT NULL,
            description TEXT NULL,
            affiliatelink TEXT NULL,
            producturl TEXT NULL,
            commissionrate TEXT NULL,
            shippingcost TEXT NULL,
            storename TEXT NULL,
            reviewcount INTEGER NOT NULL,
            reviewrate TEXT NULL,
            attributes TEXT NULL,
            ishotproduct INTEGER NOT NULL,
            isactive INTEGER NOT NULL,
            lastupdated TEXT NOT NULL
        );");
}

// এরর হ্যান্ডলিং
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

// রাউটিং ম্যাপ
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();