namespace ContentAPI.Services
{
    public interface ICacheClient
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, string[]? tags = null);
        Task RemoveAsync(string key);
        Task RemoveByTagAsync(string tag);
    }
}