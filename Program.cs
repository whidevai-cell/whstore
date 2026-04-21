using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;
using whstore.Models; 

var builder = WebApplication.CreateBuilder(args);

// ১. ডাটাবেস কানেকশন সেটআপ (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)); 

// ২. বড় সাইজের ইমেজ সাপোর্ট
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ৫. ডাটাবেস সেফটি চেক (এখানে একটু পরিবর্তন করা হয়েছে যেন অ্যাপ ক্র্যাশ না করে)
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        // ডাটাবেস কানেক্ট হতে দেরি হলে বা সমস্যা হলে এখানে এরর দেখাবে কিন্তু সাইট বন্ধ হবে না
        Console.WriteLine("Database Connection Warning: " + ex.Message);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

// ৭. এপিআই সিকিউরিটি হেডার
app.Use(async (context, next) =>
{
    if (!context.Response.Headers.ContainsKey("X-System-ID"))
    {
        context.Response.Headers.Append("X-System-ID", "AI-DASH-HALAL-786");
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// --- ডাটাবেস কন্টেক্সট ---
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<ProductModel> Products { get; set; }
}
