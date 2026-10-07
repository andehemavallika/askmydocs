using AskMyDocs.Data;
using AskMyDocs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AskMyDocs.Controllers;

public record ChatRequest(string Question, int TopK = 3, double MinSimilarity = 0.45);

[ApiController]
[Route("chat")]
public class ChatController(AskMyDocsDbContext db, EmbeddingService embedder, ChatService chat)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask(ChatRequest req)
    {
        var q = new Vector(await embedder.EmbedAsync(req.Question));

        var chunks = await db.DocumentChunks
            .Where(c => c.Embedding != null
                     && c.Embedding.CosineDistance(q) <= 1 - req.MinSimilarity)
            .OrderBy(c => c.Embedding!.CosineDistance(q))
            .Take(req.TopK)
            .Select(c => new { c.DocumentName, c.PageNumber, c.Content })
            .ToListAsync();

        if (chunks.Count == 0)
            return Ok(new
            {
                Answer = "I don't know. I couldn't find this in the uploaded documents.",
                Sources = Array.Empty<object>()
            });

        var context = string.Join("\n\n", chunks.Select((c, i) =>
            $"[Source {i + 1}: {c.DocumentName}, page {c.PageNumber}]\n{c.Content}"));

        var system = "Answer only using the context provided. " +
                     "If the answer is not in the context, say you don't know. " +
                     "Be concise and mention the page number you used.";
        var user = $"Context:\n{context}\n\nQuestion: {req.Question}";

        var answer = await chat.AskAsync(system, user);

        return Ok(new
        {
            Answer = answer,
            Sources = chunks.Select(c => new { c.DocumentName, c.PageNumber })
        });
    }
}