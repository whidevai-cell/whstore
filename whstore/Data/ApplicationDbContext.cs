using System;
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
                entity.HasKey(e => e.Id);

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
                entity.Property(e => e.ReviewCount).HasColumnName("reviewcount");
                entity.Property(e => e.ReviewRate).HasColumnName("reviewrate");
                entity.Property(e => e.Attributes).HasColumnName("attributes");
                entity.Property(e => e.IsHotProduct).HasColumnName("ishotproduct");
                entity.Property(e => e.IsActive).HasColumnName("isactive");

                entity.Property(e => e.LastUpdated)
                      .HasColumnName("lastupdated")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}