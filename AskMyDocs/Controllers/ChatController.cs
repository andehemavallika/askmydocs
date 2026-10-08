using AskMyDocs.Data;
using AskMyDocs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text;

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
                $"[{i + 1}]\n{c.Content}"));

        var system = "Answer only using the context provided. " +
             "If the answer is not in the context, say you don't know. " +
             "State only what the text explicitly says. " +
             "Do not combine facts from different roles or projects. " +
             "Be concise. After each fact, cite the source number in square brackets, like [1]. " +
             "Do not write page numbers."+
             "Every sentence or bullet must end with a source number like [1]. Keep separate roles and projects separate.";
        var user = $"Context:\n{context}\n\nQuestion: {req.Question}";

        var answer = await chat.AskAsync(system, user);

        var used = System.Text.RegularExpressions.Regex.Matches(answer, @"\[(\d+)\]")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Where(n => n >= 1 && n <= chunks.Count)
            .Distinct()
            .Select(n => chunks[n - 1])
            .Select(c => new { c.DocumentName, c.PageNumber })
            .Distinct()
            .ToList();

        if (used.Count == 0)
            used = chunks.Select(c => new { c.DocumentName, c.PageNumber }).Distinct().ToList();

        return Ok(new { Answer = answer, Sources = used });
    }


    [HttpPost("stream")]
    public async Task Stream(ChatRequest req, CancellationToken ct)
    {
        var q = new Vector(await embedder.EmbedAsync(req.Question));

        var chunks = await db.DocumentChunks
            .Where(c => c.Embedding != null
                     && c.Embedding.CosineDistance(q) <= 1 - req.MinSimilarity)
            .OrderBy(c => c.Embedding!.CosineDistance(q))
            .Take(req.TopK)
            .Select(c => new { c.DocumentName, c.PageNumber, c.Content })
            .ToListAsync(ct);

        Response.ContentType = "text/plain; charset=utf-8";

        if (chunks.Count == 0)
        {
            await Response.WriteAsync("I don't know. I couldn't find this in the uploaded documents.", ct);
            return;
        }

        var context = string.Join("\n\n", chunks.Select((c, i) => $"[{i + 1}]\n{c.Content}"));

        var system = "Answer only using the context provided. " +
                     "If the answer is not in the context, say you don't know. " +
                     "State only what the text explicitly says. " +
                     "Do not combine facts from different roles or projects. " +
                     "Be concise. Every sentence must end with a source number like [1]. " +
                     "Do not write page numbers.";
        var user = $"Context:\n{context}\n\nQuestion: {req.Question}";

        var full = new StringBuilder();
        await foreach (var token in chat.AskStreamAsync(system, user, ct))
        {
            full.Append(token);
            await Response.WriteAsync(token, ct);
            await Response.Body.FlushAsync(ct);
        }

        var used = System.Text.RegularExpressions.Regex.Matches(full.ToString(), @"\[(\d+)\]")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Where(n => n >= 1 && n <= chunks.Count)
            .Distinct()
            .Select(n => chunks[n - 1])
            .Select(c => $"{c.DocumentName} (page {c.PageNumber})")
            .Distinct()
            .ToList();

        if (used.Count == 0)
            used = chunks.Select(c => $"{c.DocumentName} (page {c.PageNumber})").Distinct().ToList();

        await Response.WriteAsync("\n\nSources: " + string.Join("; ", used), ct);
    }
}
