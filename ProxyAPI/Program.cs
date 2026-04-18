using OllamaSharp;
using Scalar.AspNetCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient("ContentApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7076");
});

var ollamaUrl = new Uri("https://ollama.com");
var apiKey = builder.Configuration["OllamaApiKey"] ?? throw new Exception("Ollama API key is not configured.");

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
