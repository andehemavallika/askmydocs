using AskMyDocs.Services;
using Microsoft.AspNetCore.Mvc;

namespace AskMyDocs.Controllers;

[ApiController]
[Route("embeddings")]
public class EmbeddingsController(EmbeddingService embedder) : ControllerBase
{
    [HttpGet("test")]
    public async Task<IActionResult> Test(string text)
    {
        var v = await embedder.EmbedAsync(text);
        return Ok(new { Dimensions = v.Length, First5 = v.Take(5) });
    }
}