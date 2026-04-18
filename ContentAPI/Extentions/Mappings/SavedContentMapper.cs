using ContentAPI.Models.SavedContent;
using ContentAPI.Models.SavedContent.DTOs;

namespace ContentAPI.Extentions.Mappings
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
                Tone = request.Tone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}