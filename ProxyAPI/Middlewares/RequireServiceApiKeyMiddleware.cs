namespace ProxyAPI.Middlewares
{
    public class RequireServiceApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequireServiceApiKeyMiddleware> _logger;
        private readonly string? _expectedApiKey;

        public RequireServiceApiKeyMiddleware(RequestDelegate next, ILogger<RequireServiceApiKeyMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _expectedApiKey = configuration["Service:ApiKey"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (string.IsNullOrWhiteSpace(_expectedApiKey))
            {
                // If middleware not configured, allow through (helps local dev if key not set)
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Service-Api-Key", out var provided) || provided != _expectedApiKey)
            {
                _logger.LogWarning("Rejected request due to missing or invalid service API key on path {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                return;
            }

            await _next(context);
        }
    }
}