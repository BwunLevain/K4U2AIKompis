using Microsoft.AspNetCore.Mvc;
using OllamaSharp;
using OllamaSharp.Models;
using ProxyAPI.DTOs;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IOllamaApiClient _ollama;
    private readonly ILogger<AiController> _logger;

    public AiController(IOllamaApiClient ollama, ILogger<AiController> logger)
    {
        _ollama = ollama;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> AskAi([FromBody] AiPromptRequest requestModel)
    {
        _ollama.SelectedModel = "gemma3:4b";

        string strictJsonSchema = """
        {
          "type": "object",
          "properties": {
            "headline": {
              "type": "string",
              "description": "A clear, concise markdown heading for the content"
            },
            "paragraphs": {
              "type": "array",
              "items": {
                "type": "string"
              },
              "description": "An array containing maximum 3 paragraphs of response text"
            },
            "uncertaintyFlag": {
              "type": "boolean",
              "description": "Set to true ONLY if the prompt asks about unknown facts, fictional contexts outside knowledge, or contains severe contradictions."
            }
          },
          "required": ["headline", "paragraphs", "uncertaintyFlag"]
        }
        """;

        var request = new GenerateRequest
        {
            Prompt = requestModel.Prompt,
            Format = strictJsonSchema,
            Stream = false
        };

        var fullResponse = new StringBuilder();

        try
        {
            await foreach (var stream in _ollama.GenerateAsync(request))
            {
                if (stream?.Response != null)
                {
                    fullResponse.Append(stream.Response);
                }
            }

            var cleanJson = fullResponse.ToString().Trim();

            return new ContentResult
            {
                Content = cleanJson,
                ContentType = "application/json",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama engine failed to generate structured response.");
            throw;
        }
    }
}