using AskMyDocs.Data;
using AskMyDocs.Models;
using AskMyDocs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace AskMyDocs.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController : ControllerBase
{
    private readonly Chunker _chunker;
    private readonly AskMyDocsDbContext _db;
    private readonly EmbeddingService _embedder;

    public DocumentsController(Chunker chunker, AskMyDocsDbContext db, EmbeddingService embedder)
    {
        _chunker = chunker;
        _db = db;
        _embedder = embedder;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please upload a PDF file.");

        using var stream = file.OpenReadStream();
        using var pdf = PdfDocument.Open(stream);

        var entities = new List<DocumentChunk>();
        foreach (var page in pdf.GetPages())
        {
            foreach (var c in _chunker.Chunk(page.Number, page.Text))
            {
                var vector = await _embedder.EmbedAsync(c.Text);
                entities.Add(new DocumentChunk
                {
                    DocumentName = file.FileName,
                    PageNumber = c.PageNumber,
                    ChunkIndex = c.ChunkIndex,
                    Content = c.Text,
                    Embedding = new Pgvector.Vector(vector)
                });
            }
        }

        _db.DocumentChunks.AddRange(entities);
        await _db.SaveChangesAsync();

        return Ok(new { Document = file.FileName, SavedChunks = entities.Count });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var docs = await _db.DocumentChunks
            .GroupBy(c => c.DocumentName)
            .Select(g => new { Document = g.Key, Chunks = g.Count() })
            .ToListAsync();

        return Ok(docs);
    }
}