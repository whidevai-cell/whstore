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
        public DbSet<EmbedModel> Embeds { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductModel>(entity =>
            {
                entity.ToTable("products");

                // Primary Key
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

                // Properties
                entity.Property(e => e.Title).HasColumnName("title").IsRequired();
                entity.Property(e => e.Category).HasColumnName("category");
                entity.Property(e => e.Price).HasColumnName("price").IsRequired();
                entity.Property(e => e.OriginalPrice).HasColumnName("originalprice");
                entity.Property(e => e.ImageUrl).HasColumnName("imageurl");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.AffiliateLink).HasColumnName("affiliatelink");
                entity.Property(e => e.ProductUrl).HasColumnName("producturl");
                entity.Property(e => e.CommissionRate).HasColumnName("commissionrate");
                entity.Property(e => e.ShippingCost).HasColumnName("shippingcost");
                entity.Property(e => e.StoreName).HasColumnName("storename");
                entity.Property(e => e.ReviewCount).HasColumnName("reviewcount");
                entity.Property(e => e.ReviewRate).HasColumnName("reviewrate");
                entity.Property(e => e.Attributes).HasColumnName("attributes");

                // Boolean properties (SQLite handles these automatically)
                entity.Property(e => e.IsHotProduct).HasColumnName("ishotproduct");
                entity.Property(e => e.IsActive).HasColumnName("isactive");

                // LastUpdated
                entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
            });
        }
    }
}