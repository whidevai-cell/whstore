using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google; // গুগল অথেনটিকেশনের জন্য যোগ করা হয়েছে
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using whstore.Services; // রিপোজিটরির নেমস্পেস

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

// ফর্ম অপশনস
builder.Services.Configure<FormOptions>(options => {
    options.ValueLengthLimit = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();

// --- 🔐 অথেন্টিকেশন ও গুগল লগইন সার্ভিস (আপডেট করা হয়েছে) ---
builder.Services.AddAuthentication(options =>
{
    // ডিফল্ট স্কিম হিসেবে কুকি সেট করা হয়েছে
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options => {
    options.LoginPath = "/Home/Index";
    options.AccessDeniedPath = "/Home/Error";
})
.AddGoogle(googleOptions => {
    // appsettings.json থেকে আইডি এবং নতুন সিক্রেট রিড করবে
    googleOptions.ClientId = configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
});

var app = builder.Build();

// মিডলওয়্যার
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
// app.UseHttpsRedirection(); // Docker/Railway deployments can fail with this enabled
app.UseStaticFiles();
app.UseRouting();

// মিডলওয়্যারের সিকোয়েন্স ঠিক আছে (Authentication আগে, তারপর Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// এই লাইনটি অবশ্যই থাকতে হবে, নাহলে অ্যাপ রান হবে না:
app.Run();