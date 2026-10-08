using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
namespace AskMyDocs.Services;

public class ChatService(HttpClient http)
{
    private record Msg(string Role, string Content);
    private record ChatResponse(Msg Message);

    public async Task<string> AskAsync(string system, string user)
    {
        var res = await http.PostAsJsonAsync("/api/chat", new
        {
            model = "llama3.2",
            stream = false,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<ChatResponse>();
        return body!.Message.Content;
    }
    public async IAsyncEnumerable<string> AskStreamAsync(
    string system, string user,
    [EnumeratorCancellation] CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(new
            {
                model = "llama3.2",
                stream = true,
                options = new { temperature = 0 },
                messages = new[]
                {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
            })
        };

        using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("message", out var m) &&
                m.TryGetProperty("content", out var c))
            {
                var text = c.GetString();
                if (!string.IsNullOrEmpty(text)) yield return text;
            }
        }
    }
}