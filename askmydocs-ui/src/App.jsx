import { useState, useEffect } from "react";

const API = "http://localhost:5041";

export default function App() {
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");
  const [sources, setSources] = useState("");
  const [loading, setLoading] = useState(false);

  const [docs, setDocs] = useState([]);
  const [selectedDoc, setSelectedDoc] = useState("");
  const [uploading, setUploading] = useState(false);
  const [uploadMsg, setUploadMsg] = useState("");

  async function loadDocs() {
    const res = await fetch(`${API}/documents`);
    setDocs(await res.json());
  }

  useEffect(() => {
    loadDocs();
  }, []);

  async function upload(e) {
    const file = e.target.files[0];
    if (!file) return;

    setUploading(true);
    setUploadMsg("Uploading and embedding. This can take a while...");

    const form = new FormData();
    form.append("file", file);

    try {
      const res = await fetch(`${API}/documents`, {
        method: "POST",
        body: form,
      });
      if (!res.ok) throw new Error("Upload failed");
      const data = await res.json();
      setUploadMsg(`Saved ${data.savedChunks} chunks from ${data.document}`);
      await loadDocs();
    } catch {
      setUploadMsg("Upload failed. Is the API running?");
    }

    setUploading(false);
    e.target.value = "";
  }

  async function ask() {
    setAnswer("");
    setSources("");
    setLoading(true);

    const res = await fetch(`${API}/chat/stream`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question, documentName: selectedDoc || null }),
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

      <h3>Documents</h3>
      <input type="file" accept=".pdf" onChange={upload} disabled={uploading} />
      <p>{uploadMsg}</p>
      <ul>
        {docs.map((d) => (
          <li key={d.document}>
            {d.document} ({d.chunks} chunks)
          </li>
        ))}
      </ul>

      <h3>Ask a question</h3>
      <select
        value={selectedDoc}
        onChange={(e) => setSelectedDoc(e.target.value)}
      >
        <option value="">All documents</option>
        {docs.map((d) => (
          <option key={d.document} value={d.document}>
            {d.document}
          </option>
        ))}
      </select>
      <br />
      <input
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        placeholder="Ask about your documents..."
        style={{ width: "80%", padding: 8, marginTop: 8 }}
      />
      <button onClick={ask} disabled={loading || !question}>
        {loading ? "Thinking..." : "Ask"}
      </button>
      <p style={{ whiteSpace: "pre-wrap" }}>{answer}</p>
      {sources && <small>Sources:{sources}</small>}
    </div>
  );
}
