using Microsoft.EntityFrameworkCore;
using AskMyDocs.Models;

namespace AskMyDocs.Data;

public class AskMyDocsDbContext : DbContext
{
    public AskMyDocsDbContext(DbContextOptions<AskMyDocsDbContext> options)
        : base(options)
    {
    }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<DocumentChunk>()
            .Property(c => c.Embedding)
            .HasColumnType("vector(768)");
    }
}