import { useState } from "react";

const API = "http://localhost:5041";

export default function App() {
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");
  const [sources, setSources] = useState("");
  const [loading, setLoading] = useState(false);

  async function ask() {
    setAnswer("");
    setSources("");
    setLoading(true);

    const res = await fetch(`${API}/chat/stream`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question }),
    });

    const reader = res.body.getReader();
    const decoder = new TextDecoder();
    let text = "";

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      text += decoder.decode(value, { stream: true });
      const [body, src] = text.split("\n\nSources:");
      setAnswer(body);
      setSources(src ?? "");
    }
    setLoading(false);
  }

  return (
    <div
      style={{ maxWidth: 700, margin: "40px auto", fontFamily: "sans-serif" }}
    >
      <h1>AskMyDocs</h1>
      <input
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        placeholder="Ask about your documents..."
        style={{ width: "80%", padding: 8 }}
      />
      <button onClick={ask} disabled={loading || !question}>
        {loading ? "Thinking..." : "Ask"}
      </button>
      <p style={{ whiteSpace: "pre-wrap" }}>{answer}</p>
      {sources && <small>Sources:{sources}</small>}
    </div>
  );
}
