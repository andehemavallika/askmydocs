using Pgvector;

namespace AskMyDocs.Models;

public class DocumentChunk
{
    public int Id { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public Vector? Embedding { get; set; }
}