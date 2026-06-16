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

        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<EmbedModel> Embeds { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                // টেবিলের নাম ডাটাবেস অনুযায়ী "Products" নিশ্চিত করা হয়েছে
                entity.ToTable("Products");

                // Primary Key ম্যাপিং
                entity.HasKey(e => e.Id);

                // এখানে "id" এর পরিবর্তে "Id" ব্যবহার করা হয়েছে কারণ আপনার DB সম্ভবত "Id" আশা করছে
                entity.Property(e => e.Id).HasColumnName("Id");

                // Properties - প্রতিটি কলামের নাম ডাটাবেসের সাথে মিলিয়ে নিন
                entity.Property(e => e.Title).HasColumnName("Title").IsRequired();
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.Price).HasColumnName("Price");
                entity.Property(e => e.OriginalPrice).HasColumnName("OriginalPrice");
                entity.Property(e => e.ImageUrl).HasColumnName("ImageUrl");
                entity.Property(e => e.Description).HasColumnName("Description");
                entity.Property(e => e.AffiliateLink).HasColumnName("AffiliateLink");
                entity.Property(e => e.ProductUrl).HasColumnName("ProductUrl");
                entity.Property(e => e.CommissionRate).HasColumnName("CommissionRate");
                entity.Property(e => e.ShippingCost).HasColumnName("ShippingCost");
                entity.Property(e => e.StoreName).HasColumnName("StoreName");

                // ReviewCount - ডাটাবেস টাইপের সাথে মিল রেখে
                entity.Property(e => e.ReviewCount).HasColumnName("ReviewCount").HasColumnType("integer");

                entity.Property(e => e.ReviewRate).HasColumnName("ReviewRate");
                entity.Property(e => e.Attributes).HasColumnName("Attributes");

                // Boolean properties - ডাটাবেস টাইপের সাথে মিল রেখে
                entity.Property(e => e.IsHotProduct).HasColumnName("IsHotProduct").HasColumnType("boolean");
                entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("boolean");

                // LastUpdated
                entity.Property(e => e.LastUpdated).HasColumnName("LastUpdated");
            });
        }
    }
}