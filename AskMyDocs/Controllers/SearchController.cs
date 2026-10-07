using AskMyDocs.Data;
using AskMyDocs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AskMyDocs.Controllers;

public record SearchRequest(string Question, int TopK = 3, double MinSimilarity = 0.45);

[ApiController]
[Route("search")]
public class SearchController(AskMyDocsDbContext db, EmbeddingService embedder) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Search(SearchRequest req)
    {
        var q = new Vector(await embedder.EmbedAsync(req.Question));

        var results = await db.DocumentChunks
            .Where(c => c.Embedding != null && c.Embedding.CosineDistance(q) <= 1 - req.MinSimilarity)
            .OrderBy(c => c.Embedding!.CosineDistance(q))
            .Take(req.TopK)
            .Select(c => new
            {
                c.DocumentName,
                c.PageNumber,
                c.ChunkIndex,
                Similarity = 1 - c.Embedding!.CosineDistance(q),
                Preview = c.Content.Substring(0, Math.Min(200, c.Content.Length))
            })
            .ToListAsync();

        return Ok(results);
    }
}