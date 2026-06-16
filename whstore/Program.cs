using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MongoDB.Driver;
using whstore.Data;
using System;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// --- MongoDB সার্ভিস ---
var mongoConnectionString = configuration.GetConnectionString("MongoConnection");
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

// --- ডাটাবেস সার্ভিস (PostgreSQL) ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        options.UseNpgsql(BuildPostgresConnectionString(databaseUrl));
    }
    else
    {
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
    }
});

// মেথড: PostgreSQL স্ট্রিং বিল্ডার
static string BuildPostgresConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
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
    return builder.ConnectionString;
}

// ফর্ম অপশনস
builder.Services.Configure<FormOptions>(options => {
    options.ValueLengthLimit = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();

// অথেন্টিকেশন
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/Error";
    });

var app = builder.Build();

// ডাটাবেস মাইগ্রেশন লাইনটি মুছে ফেলা হয়েছে যাতে আর এরর না দেয়
// ম্যানুয়ালি টেবিল তৈরি করা আছে বলে এটি আর দরকার নেই

// মিডলওয়্যার
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();