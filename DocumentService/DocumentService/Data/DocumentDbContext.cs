using DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Data;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(
        DbContextOptions<DocumentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(d => d.Description)
                .HasMaxLength(1000);

            entity.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(d => d.FileType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.FilePath)
                .HasMaxLength(500);

            entity.Property(d => d.CreatedAt)
                .IsRequired();

            entity.Property(d => d.CreatedBy)
                .IsRequired();

            entity.Property(d => d.IsActive)
                .IsRequired();
        });
    }
}