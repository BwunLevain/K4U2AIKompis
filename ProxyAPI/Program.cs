using OllamaSharp;
using Scalar.AspNetCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// Configure ContentApiClient base address from configuration and validate
var contentApiBase = builder.Configuration["ContentApi:BaseAddress"] ?? "https://localhost:7076";
if (!Uri.IsWellFormedUriString(contentApiBase, UriKind.Absolute))
{
    throw new InvalidOperationException("Invalid ContentApi:BaseAddress configuration. Provide a valid absolute URI.");
}

builder.Services.AddHttpClient("ContentApiClient", client =>
{
    client.BaseAddress = new Uri(contentApiBase);
});

// Validate Ollama API key presence
var ollamaUrl = new Uri("https://ollama.com");
var apiKey = builder.Configuration["OllamaApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("Ollama API key is not configured. Set OllamaApiKey in configuration or environment variables.");
}

builder.Services.AddScoped<IOllamaApiClient>(sp =>
{
    var httpClient = new HttpClient { BaseAddress = ollamaUrl };
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

    return new OllamaApiClient(httpClient);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Add middleware to require a service-to-service API key for incoming requests to ProxyAPI
app.UseMiddleware<ProxyAPI.Middlewares.RequireServiceApiKeyMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
