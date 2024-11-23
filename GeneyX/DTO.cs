using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.SqlServer;
namespace GeneyX
{


    public class CrawlingConfiguration
    {
        // Define properties for the crawling configuration
        public DateTime StartCrawlingDate { get; set; }
        public int CrawlDurationMins { get; set; }
    }

    public class Publication
    {
        [Key]
        public string PMID { get; set; } = string.Empty;
        public string ArticleTitle { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public int PublishedYear { get; set; }
    }
    public class PublicationDbContext : DbContext
    {
        public PublicationDbContext(DbContextOptions<PublicationDbContext> options) : base(options) { }

        public DbSet<Publication> Publications { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("localConnection");
            }
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Publication>()
                .HasIndex(u => u.PMID)
                .IsUnique();
            base.OnModelCreating(modelBuilder);
        }
    }
}
