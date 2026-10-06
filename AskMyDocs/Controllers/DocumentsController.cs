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

    public DocumentsController(Chunker chunker, AskMyDocsDbContext db)
    {
        _chunker = chunker;
        _db = db;
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

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please upload a PDF file.");

        using var stream = file.OpenReadStream();
        using var pdf = PdfDocument.Open(stream);

        var entities = pdf.GetPages()
            .SelectMany(p => _chunker.Chunk(p.Number, p.Text))
            .Select(c => new DocumentChunk
            {
                DocumentName = file.FileName,
                PageNumber = c.PageNumber,
                ChunkIndex = c.ChunkIndex,
                Content = c.Text
            })
            .ToList();

        _db.DocumentChunks.AddRange(entities);
        await _db.SaveChangesAsync();

        return Ok(new { Document = file.FileName, SavedChunks = entities.Count });
    }
}