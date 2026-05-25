using ContentAPI.Exceptions;
using ContentAPI.Extensions;
using ContentAPI.Extensions.Mappings;
using ContentAPI.Models.Common;
using ContentAPI.Models.SavedContent;
using ContentAPI.Models.SavedContent.DTOs;
using Microsoft.Extensions.Caching.Hybrid;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContentAPI.Services
{
    public class SavedContentService : ISavedContentService
    {
        private readonly ILogger<SavedContentService> _logger;
        private readonly IAiClient _aiClient;
        private readonly ICacheClient _cache;
        private readonly ISavedContentRepository _repository;

        private static int _nextId = 11;

        public SavedContentService(
            ILogger<SavedContentService> logger,
            IAiClient aiClient,
            ICacheClient cache,
            ISavedContentRepository repository)
        {
            _logger = logger;
            _aiClient = aiClient;
            _cache = cache;
            _repository = repository;
        }

        private record AiContent(string Markdown, string Headline, string[] Paragraphs, bool UncertaintyFlag);

        private async Task<AiContent> GenerateAiContentAsync(string prompt, string tone)
        {
            var rawJsonString = await _aiClient.GenerateAsync(prompt, tone);

            if (string.IsNullOrWhiteSpace(rawJsonString))
            {
                throw new HttpRequestException("The AI server returned an empty text response.", null, HttpStatusCode.BadGateway);
            }

            var cleanJson = rawJsonString.Trim();
            if (cleanJson.StartsWith("```"))
            {
                // Remove starting backticks and optional 'json' identifier
                int firstNewLine = cleanJson.IndexOf('\n');
                if (firstNewLine != -1)
                {
                    cleanJson = cleanJson.Substring(firstNewLine).Trim();
                }

                // Remove ending backticks
                if (cleanJson.EndsWith("```"))
                {
                    cleanJson = cleanJson.Substring(0, cleanJson.Length - 3).Trim();
                }
            }

            try
            {
                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                bool isUncertain = root.GetProperty("uncertaintyFlag").GetBoolean();
                var headline = root.GetProperty("headline").GetString() ?? string.Empty;

                var paragraphsList = new List<string>();
                foreach (var element in root.GetProperty("paragraphs").EnumerateArray())
                {
                    paragraphsList.Add(element.GetString() ?? string.Empty);
                }

                var structuredMarkdown = $"## {headline}\n\n" + string.Join("\n\n", paragraphsList);
                return new AiContent(structuredMarkdown, headline, paragraphsList.ToArray(), isUncertain);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse structured json output format. Building fallback structured text. Cleaned text: {CleanedText}", cleanJson);

                var raw = cleanJson;

                var paragraphs = raw.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                if (paragraphs.Count == 0)
                {
                    paragraphs = raw.Split('\n')
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();
                }

                if (paragraphs.Count == 0)
                {
                    var sentences = System.Text.RegularExpressions.Regex.Split(raw, "(?<=[.!?])\\s+")
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    paragraphs = new List<string>();
                    for (int i = 0; i < Math.Min(3, sentences.Count); i++)
                    {
                        paragraphs.Add(sentences[i]);
                    }
                }

                if (paragraphs.Count > 3)
                {
                    paragraphs = paragraphs.Take(3).ToList();
                }

                string headline = "Generated Content";
                if (paragraphs.Count > 0)
                {
                    var first = paragraphs[0];
                    var idx = first.IndexOfAny(new[] { '.', '?', '!' });
                    if (idx > 0)
                    {
                        headline = first.Substring(0, idx).Trim();
                    }
                    else
                    {
                        headline = first.Length <= 60 ? first : first.Substring(0, 57).Trim() + "...";
                    }
                }

                bool uncertain = raw.IndexOf("uncertain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 raw.IndexOf("i am not sure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 raw.IndexOf("i'm not sure", StringComparison.OrdinalIgnoreCase) >= 0;

                var sb = new System.Text.StringBuilder();
                if (uncertain)
                {
                    sb.AppendLine("### System Warning\nI am uncertain about this topic. The request may be outside verified knowledge scopes.\n");
                }

                sb.Append("## ").AppendLine(headline).AppendLine();
                sb.Append(string.Join("\n\n", paragraphs));

                return new AiContent(sb.ToString(), headline, paragraphs.ToArray(), uncertain);
            }
        }

        public async Task<SavedContentResponse> GetSavedContentByIdAsync(int id)
        {
            string cacheKey = $"content:{id}";

            return await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                _logger.LogInformation("Cache miss för ID {Id}", id);
                var target = await _repository.GetByIdAsync(id);
                if (target is null)
                {
                    throw new NotFoundException($"ID {id} hittades inte.");
                }

                return target.ToResponse();
            });
        }

        public async Task<SavedContentResponse> CreateSavedContentAsync(CreateSavedContentRequest request)
        {
            var entity = request.ToEntity();
            var ai = await GenerateAiContentAsync(request.Prompt, request.Tone);
            entity.Content = ai.Markdown;
            entity.Headline = ai.Headline;
            entity.Paragraphs = ai.Paragraphs;
            entity.UncertaintyFlag = ai.UncertaintyFlag;
            entity.Id = System.Threading.Interlocked.Increment(ref _nextId);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);

            await _cache.RemoveByTagAsync("content-list");

            return entity.ToResponse();
        }

        public async Task<bool> UpdateSavedContentAsync(int id, UpdateSavedContentRequest request)
        {
            var existing = await _repository.GetByIdAsync(id) ?? throw new NotFoundException($"ID {id} hittades inte.");

            bool needsNewAiContent = (request.Prompt != null && request.Prompt != existing.Prompt) ||
                                     (request.Tone != null && request.Tone != existing.Tone);

            var updated = new SavedContent
            {
                Id = existing.Id,
                Title = request.Title ?? existing.Title,
                Prompt = request.Prompt ?? existing.Prompt,
                Tone = request.Tone ?? existing.Tone,
                Content = existing.Content,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            if (needsNewAiContent)
            {
                var ai = await GenerateAiContentAsync(updated.Prompt, updated.Tone);
                updated.Content = ai.Markdown;
                updated.Headline = ai.Headline;
                updated.Paragraphs = ai.Paragraphs;
                updated.UncertaintyFlag = ai.UncertaintyFlag;
            }

            await _repository.UpdateAsync(updated);

            await _cache.RemoveAsync($"content:{id}");
            await _cache.RemoveByTagAsync("content-list");

            return true;
        }

        public async Task<bool> DeleteSavedContentAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                throw new NotFoundException($"ID {id} hittades inte.");
            }

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

                var all = await _repository.GetAllAsync();
                var query = all.ToList().AsQueryable();

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