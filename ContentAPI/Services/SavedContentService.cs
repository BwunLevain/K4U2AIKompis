using ContentAPI.Exceptions;
using ContentAPI.Extensions;
using ContentAPI.Extentions.Mappings;
using ContentAPI.Models.Common;
using ContentAPI.Models.SavedContent;
using ContentAPI.Models.SavedContent.DTOs;
using Microsoft.Extensions.Caching.Hybrid;

namespace ContentAPI.Services
{
    public class SavedContentService : ISavedContentService
    {
        private readonly ILogger<SavedContentService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HybridCache _cache;

        // In-memory lagring
        private static readonly List<SavedContent> _savedContent = Enumerable.Range(1, 10).Select(i => new SavedContent
        {
            Id = i,
            Title = $"Saved Content {i}",
            Prompt = $"This is the prompt for saved content {i}.",
            Content = $"Initial AI Content {i}.",
            Tone = i % 2 == 0 ? "Professional" : "Informative",
            CreatedAt = DateTime.UtcNow.AddDays(-i),
            UpdatedAt = DateTime.UtcNow.AddDays(-i / 2)
        }).ToList();

        private static int _nextId = 11;

        public SavedContentService(
            ILogger<SavedContentService> logger,
            IHttpClientFactory httpClientFactory,
            HybridCache cache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        private async Task<string> GenerateAiContentAsync(string prompt, string tone)
        {
            try
            {
                var formattedPrompt = $"answer this: {prompt} In this Tone: {tone}";
                var client = _httpClientFactory.CreateClient("ProxyApiClient");

                var response = await client.PostAsJsonAsync("api/ai/ask", formattedPrompt);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogWarning("AI Service returnerade status: {StatusCode}", response.StatusCode);
                return "Error: Kunde inte generera innehåll.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tekniskt fel vid kontakt med AI-tjänsten.");
                return "Error: AI-tjänsten är otillgänglig.";
            }
        }

        public async Task<SavedContentResponse> GetSavedContentByIdAsync(int id)
        {
            string cacheKey = $"content:{id}";

            return await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                _logger.LogInformation("Cache miss för ID {Id}", id);
                var target = _savedContent.FirstOrDefault(p => p.Id == id)
                             ?? throw new NotFoundException($"ID {id} hittades inte.");

                return target.ToResponse();
            });
        }

        public async Task<SavedContentResponse> CreateSavedContentAsync(CreateSavedContentRequest request)
        {
            var entity = request.ToEntity();
            entity.Content = await GenerateAiContentAsync(request.Prompt, request.Tone);
            entity.Id = _nextId++;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _savedContent.Add(entity);

            await _cache.RemoveByTagAsync("content-list");

            return entity.ToResponse();
        }

        public async Task<bool> UpdateSavedContentAsync(int id, UpdateSavedContentRequest request)
        {
            var target = _savedContent.FirstOrDefault(p => p.Id == id)
                         ?? throw new NotFoundException($"ID {id} hittades inte.");

            bool needsNewAiContent = (request.Prompt != null && request.Prompt != target.Prompt) ||
                                     (request.Tone != null && request.Tone != target.Tone);

            target.Title = request.Title ?? target.Title;
            target.Prompt = request.Prompt ?? target.Prompt;
            target.Tone = request.Tone ?? target.Tone;
            target.UpdatedAt = DateTime.UtcNow;

            if (needsNewAiContent)
            {
                target.Content = await GenerateAiContentAsync(target.Prompt, target.Tone);
            }

            await _cache.RemoveAsync($"content:{id}");
            await _cache.RemoveByTagAsync("content-list");

            return true;
        }

        public async Task<bool> DeleteSavedContentAsync(int id)
        {
            var target = _savedContent.FirstOrDefault(p => p.Id == id)
                ?? throw new NotFoundException($"ID {id} hittades inte.");

            _savedContent.Remove(target);

            await _cache.RemoveAsync($"content:{id}");
            await _cache.RemoveByTagAsync("content-list");

            return true;
        }

        public async Task<PagedResponse<SavedContentResponse>> GetPagedResponseAsync(SavedContentFilter filter)
        {
            string cacheKey = $"list_p{filter.Page}_s{filter.PageSize}_sort{filter.Sort ?? "none"}_t{filter.Tone ?? "any"}";

            return await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                _logger.LogInformation("Genererar nytt paginerat svar för cache...");

                var query = _savedContent.ToList().AsQueryable();

                // Filtrering
                if (filter.CreatedAt.HasValue)
                    query = query.Where(c => c.CreatedAt >= filter.CreatedAt.Value);

                if (!string.IsNullOrWhiteSpace(filter.Tone))
                    query = query.Where(c => c.Tone.Contains(filter.Tone, StringComparison.OrdinalIgnoreCase));

                // Sortering
                query = filter.Sort?.ToLower() switch
                {
                    "createdat" => query.OrderBy(c => c.CreatedAt),
                    "-createdat" => query.OrderByDescending(c => c.CreatedAt),
                    "title" => query.OrderBy(c => c.Title),
                    "-title" => query.OrderByDescending(c => c.Title),
                    _ => query.OrderByDescending(c => c.CreatedAt)
                };

                return query.ToPagedResponse(filter.Page, filter.PageSize, p => p.ToResponse());
            },
            tags: new[] { "content-list" });
        }
    }
}