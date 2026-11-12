using System.Text;
using System.Text.RegularExpressions;
using AskMyDocs.Data;
using AskMyDocs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AskMyDocs.Controllers;

public record ChatRequest(
    string Question,
    int TopK = 3,
    double MinSimilarity = 0.45,
    string? DocumentName = null);

[ApiController]
[Route("chat")]
public class ChatController(AskMyDocsDbContext db, EmbeddingService embedder, ChatService chat)
    : ControllerBase
{
    private record RetrievedChunk(string DocumentName, int PageNumber, string Content);

    private const string SystemPrompt =
        "Answer only using the context provided. " +
        "If the answer is not in the context, say you don't know. " +
        "State only what the text explicitly says. " +
        "Do not combine facts from different roles, projects or documents. " +
        "Be concise. Every sentence must end with a source number like [1]. " +
        "Do not write page numbers.";

    private async Task<List<RetrievedChunk>> RetrieveAsync(ChatRequest req, CancellationToken ct)
    {
        var q = new Vector(await embedder.EmbedAsync(req.Question));

        return await db.DocumentChunks
            .Where(c => c.Embedding != null
                     && (req.DocumentName == null || c.DocumentName == req.DocumentName)
                     && c.Embedding.CosineDistance(q) <= 1 - req.MinSimilarity)
            .OrderBy(c => c.Embedding!.CosineDistance(q))
            .Take(req.TopK)
            .Select(c => new RetrievedChunk(c.DocumentName, c.PageNumber, c.Content))
            .ToListAsync(ct);
    }

    private static string BuildUserPrompt(List<RetrievedChunk> chunks, string question)
    {
        var context = string.Join("\n\n", chunks.Select((c, i) => $"[{i + 1}]\n{c.Content}"));
        return $"Context:\n{context}\n\nQuestion: {question}";
    }

    private static List<string> CitedSources(string answer, List<RetrievedChunk> chunks)
    {
        var cited = Regex.Matches(answer, @"\[(\d+)\]")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Where(n => n >= 1 && n <= chunks.Count)
            .Distinct()
            .Select(n => chunks[n - 1])
            .ToList();

        if (cited.Count == 0) cited = chunks;

        return cited
            .Select(c => $"{c.DocumentName} (page {c.PageNumber})")
            .Distinct()
            .ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Ask(ChatRequest req, CancellationToken ct)
    {
        var chunks = await RetrieveAsync(req, ct);

        if (chunks.Count == 0)
            return Ok(new
            {
                Answer = "I don't know. I couldn't find this in the uploaded documents.",
                Sources = Array.Empty<string>()
            });

        var answer = await chat.AskAsync(SystemPrompt, BuildUserPrompt(chunks, req.Question));
        return Ok(new { Answer = answer, Sources = CitedSources(answer, chunks) });
    }

    [HttpPost("stream")]
    public async Task Stream(ChatRequest req, CancellationToken ct)
    {
        var chunks = await RetrieveAsync(req, ct);
        Response.ContentType = "text/plain; charset=utf-8";

        if (chunks.Count == 0)
        {
            await Response.WriteAsync("I don't know. I couldn't find this in the uploaded documents.", ct);
            return;
        }

        var full = new StringBuilder();
        await foreach (var token in chat.AskStreamAsync(SystemPrompt, BuildUserPrompt(chunks, req.Question), ct))
        {
            full.Append(token);
            await Response.WriteAsync(token, ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("\n\nSources: " + string.Join("; ", CitedSources(full.ToString(), chunks)), ct);
    }
}