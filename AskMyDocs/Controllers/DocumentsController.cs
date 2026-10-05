using AskMyDocs.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using UglyToad.PdfPig;
namespace AskMyDocs.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly Chunker _chunker;

        public DocumentsController(Chunker chunker)
        {
            _chunker = chunker;
        }
        [HttpPost]
        public IActionResult UploadDocument([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            using var stream = file.OpenReadStream();
            using var pdf = PdfDocument.Open(stream);
            var chunks = pdf.GetPages()
                .SelectMany(p => _chunker.Chunk(p.Number, p.Text))
                .ToList();

            return Ok(new { TotalChunks = chunks.Count, Chunks = chunks });
        }
    }
}
