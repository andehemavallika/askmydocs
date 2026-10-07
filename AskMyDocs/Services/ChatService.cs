using System.Net.Http.Json;

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
}