using System.Net.Http.Json;

namespace AskMyDocs.Services;

public class EmbeddingService(HttpClient http)
{
    private record EmbedResponse(float[][] Embeddings);

    public async Task<float[]> EmbedAsync(string text)
    {
        var res = await http.PostAsJsonAsync("/api/embed",
            new { model = "nomic-embed-text", input = text });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<EmbedResponse>();
        return body!.Embeddings[0];
    }
}