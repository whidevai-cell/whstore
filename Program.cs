using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;
using whstore.Models; // আপনার Model ফোল্ডারটি এখানে অ্যাড করুন

var builder = WebApplication.CreateBuilder(args);

// ১. ডাটাবেস কানেকশন সেটআপ (SQLite - Built-in DB)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// ২. বড় সাইজের ইমেজ (Base64) সাপোর্ট করার জন্য কনফিগারেশন
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

// ৩. সার্ভিস কন্টেইনারে কন্ট্রোলার এবং ভিউ অ্যাড করা
builder.Services.AddControllersWithViews();

// ৪. CORS পলিসি
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ৫. ডাটাবেস অটোমেটিক তৈরি করার জন্য
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // উল্লেখ্য: যেহেতু আপনি সরাসরি SQL দিয়ে টেবিল বানাচ্ছেন কন্ট্রোলারে, 
    // তাই EnsureCreated() শুধু প্রাথমিক স্ট্রাকচার তৈরি করবে।
    db.Database.EnsureCreated();
}

// ৬. এনভায়রনমেন্ট অনুযায়ী এরর হ্যান্ডলিং
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseRouting();

// ৭. ইউনিভার্সাল এপিআই সিকিউরিটি এবং রেসপন্স হেডার
app.Use(async (context, next) =>
{
    if (!context.Response.Headers.ContainsKey("X-System-ID"))
    {
        context.Response.Headers.Append("X-System-ID", "AI-DASH-HALAL-786");
    }

    var authorizedKeys = new List<string> { "AI-DASH-HALAL-786", "TEST-KEY-001", "DEV-HALAL-99" };

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var providedKey = context.Request.Headers["X-API-KEY"].ToString();
        if (!string.IsNullOrEmpty(providedKey) && !authorizedKeys.Contains(providedKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: Invalid AI-DASH System Key!");
            return;
        }
    }
    await next();
});

app.UseAuthorization();

// ৮. ডিফল্ট রাউট সেটআপ
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// --- ডাটাবেস কন্টেক্সট ---
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    // আপনার প্রজেক্টের মেইন ProductModel ব্যবহার করুন
    public DbSet<ProductModel> Products { get; set; }
}