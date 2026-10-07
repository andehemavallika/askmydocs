namespace AskMyDocs.Services;

public record TextChunk(int PageNumber, int ChunkIndex, string Text);

public class Chunker
{
    public List<TextChunk> Chunk(int pageNumber, string text,
                                 int chunkSize = 600, int overlap = 100)
    {
        var chunks = new List<TextChunk>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        int start = 0, index = 0;

        while (start < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - start);
            var piece = text.Substring(start, length).Trim();
            if (piece.Length > 0)
                chunks.Add(new TextChunk(pageNumber, index++, piece));

            if (start + length >= text.Length) break;
            start += chunkSize - overlap;
        }
        return chunks;
    }
}