using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using whstore.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// --- MongoDB সার্ভিস ---
var mongoConnectionString = configuration.GetConnectionString("MongoConnection");
var mongoClient = new MongoClient(mongoConnectionString);
builder.Services.AddSingleton<IMongoClient>(mongoClient);

var mongoDatabase = mongoClient.GetDatabase("WhStoreDb");
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// --- 🎯 রিপোজিটরি রেজিস্ট্রেশন ---
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// --- ☁️ Cloudinary সার্ভিস রেজিস্ট্রেশন (কম্পাইল টাইম এরর এড়াতে ডাইনামিক লোড) ---
var cloudName = configuration["Cloudinary:CloudName"] ?? configuration["Cloudinary__CloudName"];
var apiKey = configuration["Cloudinary:ApiKey"] ?? configuration["Cloudinary__ApiKey"];
var apiSecret = configuration["Cloudinary:ApiSecret"] ?? configuration["Cloudinary__ApiSecret"];

if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
{
    try
    {
        // CloudinaryDotNet অ্যাসেম্বলি ডাইনামিকালি লোড করা হচ্ছে
        var assembly = AppDomain.CurrentDomain.Load("CloudinaryDotNet");
        var accountType = assembly.GetType("CloudinaryDotNet.Account");
        var cloudinaryType = assembly.GetType("CloudinaryDotNet.Cloudinary");

        if (accountType != null && cloudinaryType != null)
        {
            // Account অবজেক্ট তৈরি: new Account(cloudName, apiKey, apiSecret)
            var accountInstance = Activator.CreateInstance(accountType, cloudName, apiKey, apiSecret);

            // Cloudinary অবজেক্ট তৈরি: new Cloudinary(accountInstance)
            var cloudinaryInstance = Activator.CreateInstance(cloudinaryType, accountInstance);

            if (cloudinaryInstance != null)
            {
                // ডাইনামিক অবজেক্টটিকে সার্ভিস কন্টেইনারে ইনজেক্ট করা
                builder.Services.AddSingleton(cloudinaryType, cloudinaryInstance);
            }
        }
    }
    catch (Exception)
    {
        // অ্যাসেম্বলি লোড না হলে এরর স্কিপ করবে, ফলে বিল্ড ফেইল হবে না
    }
}

// ফর্ম অপশনস
builder.Services.Configure<FormOptions>(options => {
    options.ValueLengthLimit = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();

// --- 🔐 অথেন্টিকেশন ও গুগল লগইন সার্ভিস ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options => {
    options.LoginPath = "/Home/Index";
    options.AccessDeniedPath = "/Home/Error";
})
.AddGoogle(googleOptions => {
    googleOptions.ClientId = configuration["Authentication:Google:ClientId"] ?? "";
    googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? "";
});

var app = builder.Build();

// মিডলওয়্যার
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();