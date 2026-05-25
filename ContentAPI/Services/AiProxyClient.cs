using System.Net;
using System.Net.Http.Json;

namespace ContentAPI.Services
{
    public class AiProxyClient : IAiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiProxyClient> _logger;

        // Simple in-memory circuit-breaker state
        private int _failureCount = 0;
        private readonly object _lock = new();
        private DateTime _circuitOpenedAt = DateTime.MinValue;
        private readonly TimeSpan _circuitBreakDuration = TimeSpan.FromSeconds(30);
        private const int FailureThreshold = 3;

        private readonly string? _serviceApiKey;

        public AiProxyClient(IHttpClientFactory httpClientFactory, ILogger<AiProxyClient> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _serviceApiKey = configuration["Service:ApiKey"];
        }

        public async Task<string> GenerateAsync(string prompt, string tone, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_failureCount >= FailureThreshold && DateTime.UtcNow - _circuitOpenedAt < _circuitBreakDuration)
                {
                    throw new HttpRequestException("AI upstream temporarily unavailable due to repeated failures.");
                }

                if (_failureCount >= FailureThreshold && DateTime.UtcNow - _circuitOpenedAt >= _circuitBreakDuration)
                {
                    // Reset after circuit break duration
                    _failureCount = 0;
                }
            }

            var client = _httpClientFactory.CreateClient("ProxyApiClient");
            var formattedPrompt = $"[SYSTEM INSTRUCTION: You are a professional content generator. Adopt a strictly {tone} tone.]\nUser Query: {prompt}";
            var payload = new { Prompt = formattedPrompt };

            int retries = 2;
            for (int attempt = 0; attempt <= retries; attempt++)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(15));

                    // Build request with service-to-service API key header
                    using var request = new HttpRequestMessage(HttpMethod.Post, "api/ai/ask")
                    {
                        Content = JsonContent.Create(payload)
                    };

                    if (!string.IsNullOrWhiteSpace(_serviceApiKey))
                    {
                        request.Headers.Add("X-Service-Api-Key", _serviceApiKey);
                    }

                    var response = await client.SendAsync(request, cts.Token);
                    response.EnsureSuccessStatusCode();

                    var raw = await response.Content.ReadAsStringAsync(cts.Token);

                    // Success: reset failure count
                    lock (_lock)
                    {
                        _failureCount = 0;
                    }

                    return raw ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI proxy call failed (attempt {Attempt}).", attempt + 1);

                    lock (_lock)
                    {
                        _failureCount++;
                        if (_failureCount >= FailureThreshold)
                        {
                            _circuitOpenedAt = DateTime.UtcNow;
                        }
                    }

                    if (attempt == retries)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }

            throw new HttpRequestException("Unexpected failure when calling AI proxy.");
        }
    }
}