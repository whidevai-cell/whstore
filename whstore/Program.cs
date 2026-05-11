using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using whstore.Models;

var builder = WebApplication.CreateBuilder(args);

// ১. PostgreSQL কানেকশন স্ট্রিং হ্যান্ডলিং
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// এনভায়রনমেন্ট ভেরিয়েবল থেকে কানেকশন স্ট্রিং চেক (Render-এর জন্য)
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ২. ডাটা ও ইমেজ হ্যান্ডলিং
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

// ৩. অটো ডাটাবেস আপডেট (EnsureCreated এর বদলে Migrate ব্যবহার করা ভালো, তবে আপনার জন্য এটি ঠিক করা হয়েছে)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // PostgreSQL-এ টেবিল কেস-সেন্সিটিভ সমস্যা এড়াতে এটি জরুরি
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// --- আপডেট করা PostgreSQL ডাটাবেস কন্টেক্সট ---
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ProductModel> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductModel>(entity =>
        {
            // PostgreSQL-এ টেবিল এবং কলামের নাম সব ছোট হাতের (lowercase) হওয়া নিরাপদ
            entity.ToTable("products");
            entity.HasKey(e => e.Id); // প্রাইমারি কি নিশ্চিত করা

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("productid");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ProductUrl).HasColumnName("producturl");
            entity.Property(e => e.AffiliateLink).HasColumnName("affiliatelink");
            entity.Property(e => e.ImageUrl).HasColumnName("imageurl");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.OriginalPrice).HasColumnName("originalprice");
            entity.Property(e => e.CommissionRate).HasColumnName("commissionrate");
            entity.Property(e => e.ShippingCost).HasColumnName("shippingcost");
            entity.Property(e => e.StoreName).HasColumnName("storename");
            entity.Property(e => e.Category).HasColumnName("category");

            // নিচের কলামগুলো যদি মডেলে না থাকে তবে এরর দিবে, তাই চেক করে নিন
            entity.Property(e => e.IsHotProduct).HasColumnName("ishotproduct");
            entity.Property(e => e.IsActive).HasColumnName("isactive");

            // DateTime টাইপ PostgreSQL-এ সামঞ্জস্য করা
            entity.Property(e => e.LastUpdated)
                  .HasColumnName("lastupdated")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}