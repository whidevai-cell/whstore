using Microsoft.EntityFrameworkCore;
using whstore.Models;

namespace whstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductModel> Products { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductModel>(entity =>
            {
                entity.ToTable("products");

                // Primary Key
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

                // Mapping properties with explicit SQL Server types
                entity.Property(e => e.Title).HasColumnName("title").HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(e => e.Category).HasColumnName("category").HasColumnType("nvarchar(max)");
                entity.Property(e => e.Price).HasColumnName("price").HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(e => e.OriginalPrice).HasColumnName("originalprice").HasColumnType("nvarchar(max)");
                entity.Property(e => e.ImageUrl).HasColumnName("imageurl").HasColumnType("nvarchar(max)");
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
                entity.Property(e => e.AffiliateLink).HasColumnName("affiliatelink").HasColumnType("nvarchar(max)");
                entity.Property(e => e.ProductUrl).HasColumnName("producturl").HasColumnType("nvarchar(max)");
                entity.Property(e => e.CommissionRate).HasColumnName("commissionrate").HasColumnType("nvarchar(max)");
                entity.Property(e => e.ShippingCost).HasColumnName("shippingcost").HasColumnType("nvarchar(max)");
                entity.Property(e => e.StoreName).HasColumnName("storename").HasColumnType("nvarchar(max)");
                entity.Property(e => e.ReviewCount).HasColumnName("reviewcount").HasColumnType("nvarchar(max)");
                entity.Property(e => e.ReviewRate).HasColumnName("reviewrate").HasColumnType("nvarchar(max)");
                entity.Property(e => e.Attributes).HasColumnName("attributes").HasColumnType("nvarchar(max)");

                // Boolean fields (SQL Server-এ bit হিসেবে কাজ করবে)
                entity.Property(e => e.IsHotProduct).HasColumnName("ishotproduct").HasColumnType("bit");
                entity.Property(e => e.IsActive).HasColumnName("isactive").HasColumnType("bit");

                // LastUpdated
                entity.Property(e => e.LastUpdated)
                      .HasColumnName("lastupdated")
                      .HasColumnType("datetime2")
                      .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}