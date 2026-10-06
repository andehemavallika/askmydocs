# AskMyDocs

A RAG-based document Q&A assistant. Upload a PDF, ask questions,
and get answers with page citations. (In progress)

## Tech Stack
- ASP.NET Core (.NET 10)
- PostgreSQL + pgvector
- Entity Framework Core
- PdfPig (PDF text extraction)

## How It Works (so far)
PDF upload -> extract text per page -> chunk (1500 chars, 200 overlap)
-> store chunks with page numbers in PostgreSQL

## Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| POST | /documents | Upload a PDF, chunk it, save to DB |
| GET | /documents | List stored documents with chunk counts |

## Run Locally
1. Install PostgreSQL with the pgvector extension
2. Set the connection string in user secrets:
   dotnet user-secrets set "ConnectionStrings:Default" "<your connection string>"
3. Apply migrations: dotnet ef database update
4. Run: dotnet run

## Design Decisions
- Chunk size 1500 chars with 200 overlap: precise retrieval, no lost context at boundaries
- Page number stored per chunk: enables citations
- Controllers + services: keeps request handling separate from logic

## Roadmap
- [x] PDF upload and text extraction
- [x] Chunking
- [x] Store chunks in PostgreSQL
- [ ] Embeddings and vector search
- [ ] LLM answers with citations
- [ ] Streaming responses
- [ ] Evaluation set and Docker
