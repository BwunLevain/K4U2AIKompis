using ContentAPI.Models.SavedContent;
using ContentAPI.Models.SavedContent.DTOs;

namespace ContentAPI.Extensions.Mappings
{
    public static class SavedContentMapper
    {
        public static SavedContentResponse ToResponse(this SavedContent savedContent)
        {
            return new SavedContentResponse(
                savedContent.Id,
                savedContent.Title,
                savedContent.Prompt,
                savedContent.Content,
                savedContent.Headline,
                savedContent.Paragraphs,
                savedContent.UncertaintyFlag,
                savedContent.Tone,
                savedContent.CreatedAt,
                savedContent.UpdatedAt
            );
        }

        public static SavedContent ToEntity(this CreateSavedContentRequest request)
        {
            return new SavedContent
            {
                Title = request.Title,
                Prompt = request.Prompt,
                Content = string.Empty,
                Headline = string.Empty,
                Paragraphs = Array.Empty<string>(),
                UncertaintyFlag = false,
                Tone = request.Tone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}