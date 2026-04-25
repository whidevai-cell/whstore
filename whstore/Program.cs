using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Reflection.Emit;
using whstore.Models;

var builder = WebApplication.CreateBuilder(args);

// ১. PostgreSQL ডাটাবেস কানেকশন সেটআপ (appsettings.json থেকে নেওয়া)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ২. বড় সাইজের ইমেজ ও ডাটা সাপোর্ট
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

builder.Services.AddControllersWithViews();

// ৩. CORS পলিসি
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ৪. ডাটাবেস টেবিল অটো-ক্রিয়েট (PostgreSQL এর জন্য)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        // কনসোলে এরর মেসেজ দেখাবে যদি ডাটাবেস কানেকশনে সমস্যা থাকে
        Console.WriteLine("Database Connection Error: " + ex.Message);
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

// ৫. কাস্টম সিকিউরিটি হেডার
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-System-ID", "WH-STORE-PRO-99");
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// --- PostgreSQL ডাটাবেস কন্টেক্সট ---
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ProductModel> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductModel>(entity =>
        {
            entity.ToTable("products"); // PostgreSQL টেবিল নাম ছোট হাতের অক্ষরে
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("productid");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.ProductUrl).HasColumnName("producturl");
            entity.Property(e => e.AffiliateLink).HasColumnName("affiliatelink");
            entity.Property(e => e.ImageUrl).HasColumnName("imageurl");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.OriginalPrice).HasColumnName("originalprice");
            entity.Property(e => e.CommissionRate).HasColumnName("commissionrate");
            entity.Property(e => e.ShippingCost).HasColumnName("shippingcost");
            entity.Property(e => e.StoreName).HasColumnName("storename");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.ReviewCount).HasColumnName("reviewcount");
            entity.Property(e => e.ReviewRate).HasColumnName("reviewrate");
            entity.Property(e => e.Attributes).HasColumnName("attributes");
            entity.Property(e => e.IsHotProduct).HasColumnName("ishotproduct");
            entity.Property(e => e.IsActive).HasColumnName("isactive");
            entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
        });
    }
}