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
}