# K4U2AIKompis

K4U2AIKompis is a distributed system consisting of two .NET 9 microservices designed to manage and generate AI-enhanced content. It features a robust architecture utilizing modern .NET features such as HybridCache, Scalar API Reference, and JWT Authentication.

## Architecture

The solution is divided into two main projects:

1.  ContentAPI: The core service that handles user authentication, CRUD operations for "Saved Content," and data persistence. It utilizes HybridCache for high-performance data retrieval and tag-based cache invalidation.
2.  ProxyAPI: Acts as an AI gateway. It interfaces with external AI providers (via OllamaSharp) to process prompts and generate text based on specific tones requested by the ContentAPI.

---

## Getting Started

### 1. Start Both Projects Simultaneously
To run the full system, both APIs must be active so they can communicate via HttpClient.

#### Using Visual Studio (Windows/Mac):
1.  Right-click on the Solution in Solution Explorer.
2.  Select Configure Startup Projects...
3.  Choose Multiple startup projects.
4.  Set the Action for both ContentAPI and ProxyAPI to Start (use https).
5.  Press F5 or click Start.

#### Using .NET CLI:
Open two separate terminal windows in the root directory:

Terminal 1 (ProxyAPI):
```bash
cd ProxyAPI
dotnet run
```

Terminal 2 (ContentAPI):
```bash
cd ContentAPI
dotnet run
```

---

### 2. Setting Up User Secrets (LLM Integration)
The ProxyAPI requires an API Key to communicate with the Ollama/LLM service. For security, this key should never be stored in appsettings.json. Instead, use .NET User Secrets.

1.  Open a terminal in the ProxyAPI project folder.
2.  Initialize user secrets if not already done:
    ```bash
    dotnet user-secrets init
    ```
3.  Add your API key (replace your_actual_key_here with your real key):
    ```bash
    dotnet user-secrets set "OllamaApiKey" "your_actual_key_here"
    ```
4.  The application will now automatically pick up this key via builder.Configuration["OllamaApiKey"] during startup.

---

## Authentication

To access protected endpoints:
1.  Send a POST request to /api/auth/login.
2.  Use the default credentials (for development):
    * Username: admin
    * Password: password123
3.  The returned token must be sent as a Bearer Token in the Authorization header for all POST, PUT, and DELETE requests.

---

## API Endpoints (ContentAPI)

| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| POST | /api/auth/login | Get JWT Token | No |
| GET | /api/savedcontent | List all (supports filter/sort/page) | No |
| POST | /api/savedcontent | Create new AI content | Yes |
| GET | /api/savedcontent/{id} | Get specific entry | No |
| PUT | /api/savedcontent/{id} | Update entry (triggers AI refresh) | Yes |
| DELETE | /api/savedcontent/{id} | Remove entry | Yes |

---

## Technical Highlights

* HybridCache: Implements the new .NET 9 cache system to handle L1/L2 caching and prevents cache stampede.
* Scalar API Docs: Interactive documentation available at https://localhost:7076/scalar.
* Optimized UI Experience: Cache invalidation occurs immediately upon update to ensure the user sees changes without AI-latency delays.
