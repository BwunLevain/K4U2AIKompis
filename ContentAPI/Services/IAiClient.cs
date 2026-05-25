namespace ContentAPI.Services
{
    public interface IAiClient
    {
        Task<string> GenerateAsync(string prompt, string tone, CancellationToken cancellationToken = default);
    }
}