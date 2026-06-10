using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using whstore.Data;
using whstore.Services;
using System;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ১. ডাটাবেস সার্ভিস রেজিস্টার (SQLite ব্যবহার করা হয়েছে)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

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