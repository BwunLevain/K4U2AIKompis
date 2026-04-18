using ContentAPI.Exceptions;
using ContentAPI.Extensions;
using ContentAPI.Extentions.Mappings;
using ContentAPI.Models.Common;
using ContentAPI.Models.SavedContent;
using ContentAPI.Models.SavedContent.DTOs;

namespace ContentAPI.Services
{
    public class SavedContentService : ISavedContentService
    {
        private readonly ILogger<SavedContentService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

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

        public SavedContentService(ILogger<SavedContentService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Privat hjälpmetod för att kommunicera med AI-kontrollern (Service B).
        /// Återanvänds vid både skapande och uppdatering.
        /// </summary>
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
                return "Error: Kunde inte generera innehåll från AI-tjänsten.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ett tekniskt fel uppstod vid kontakt med AI-tjänsten.");
                return "Error: AI-tjänsten är för tillfället otillgänglig.";
            }
        }

        public async Task<SavedContentResponse> GetSavedContentByIdAsync(int id)
        {
            var target = _savedContent.FirstOrDefault(p => p.Id == id)
                         ?? throw new NotFoundException($"ID {id} hittades inte.");

            return await Task.FromResult(target.ToResponse());
        }

        public async Task<SavedContentResponse> CreateSavedContentAsync(CreateSavedContentRequest request)
        {
            _logger.LogInformation("Skapar nytt sparat innehåll...");

            var entity = request.ToEntity();

            entity.Content = await GenerateAiContentAsync(request.Prompt, request.Tone);

            entity.Id = _nextId++;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _savedContent.Add(entity);

            return entity.ToResponse();
        }

        public async Task<bool> UpdateSavedContentAsync(int id, UpdateSavedContentRequest request)
        {
            var target = _savedContent.FirstOrDefault(p => p.Id == id)
                         ?? throw new NotFoundException($"ID {id} hittades inte.");

            bool promptChanged = request.Prompt != null && request.Prompt != target.Prompt;
            bool toneChanged = request.Tone != null && request.Tone != target.Tone;

            target.Title = request.Title ?? target.Title;
            target.Prompt = request.Prompt ?? target.Prompt;
            target.Tone = request.Tone ?? target.Tone;
            target.UpdatedAt = DateTime.UtcNow;

            if (promptChanged || toneChanged)
            {
                _logger.LogInformation("Uppdaterar AI-innehåll för ID {Id} på grund av ändrad prompt/ton.", id);
                target.Content = await GenerateAiContentAsync(target.Prompt, target.Tone);
            }

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteSavedContentAsync(int id)
        {
            var target = _savedContent.FirstOrDefault(p => p.Id == id)
                ?? throw new NotFoundException($"ID {id} hittades inte.");

            _savedContent.Remove(target);

            return await Task.FromResult(true);
        }

        public async Task<PagedResponse<SavedContentResponse>> GetPagedResponseAsync(SavedContentFilter filter)
        {
            var query = _savedContent.AsQueryable();

            // --- filter ---
            if (filter.CreatedAt.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= filter.CreatedAt.Value);
            }

            if (filter.UpdatedAt.HasValue)
            {
                query = query.Where(c => c.UpdatedAt >= filter.UpdatedAt.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Tone))
            {
                query = query.Where(c => c.Tone.Contains(filter.Tone, StringComparison.OrdinalIgnoreCase));
            }

            // --- sort ---
            query = filter.Sort?.ToLower() switch
            {
                "createdat" => query.OrderBy(c => c.CreatedAt),
                "-createdat" => query.OrderByDescending(c => c.CreatedAt),
                "updatedat" => query.OrderBy(c => c.UpdatedAt),
                "-updatedat" => query.OrderByDescending(c => c.UpdatedAt),
                "title" => query.OrderBy(c => c.Title),
                "-title" => query.OrderByDescending(c => c.Title),
                _ => query.OrderByDescending(c => c.CreatedAt) // Default
            };

            // --- Pagig ---
            var response = query.ToPagedResponse(filter.Page, filter.PageSize, p => p.ToResponse());

            return await Task.FromResult(response);
        }
    }
}