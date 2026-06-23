using Microsoft.AspNetCore.Authentication.Cookies;
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

// --- রিপোজিটরি রেজিস্ট্রেশন (এখানে যোগ করুন) ---
builder.Services.AddScoped<IProductRepository, ProductRepository>();

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

// মিডলওয়্যার
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();