using Microsoft.AspNetCore.Mvc;
using OllamaSharp;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IOllamaApiClient _ollama;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiController(IOllamaApiClient ollama, IHttpClientFactory httpClientFactory)
    {
        _ollama = ollama;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> AskAi([FromBody] string prompt)
    {
        _ollama.SelectedModel = "gemma3:4b";

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var stream in _ollama.GenerateAsync(prompt))
        {
            fullResponse.Append(stream.Response);
        }

        return Ok(fullResponse.ToString());
    }
}