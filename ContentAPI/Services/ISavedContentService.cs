using ContentAPI.Models.Common;
using ContentAPI.Models.SavedContent.DTOs;

namespace ContentAPI.Services
{
    public interface ISavedContentService
    {
        Task<SavedContentResponse> CreateSavedContentAsync(CreateSavedContentRequest request);
        Task<SavedContentResponse> GetSavedContentByIdAsync(int id);
        Task<bool> UpdateSavedContentAsync(int id, UpdateSavedContentRequest request);
        Task<PagedResponse<SavedContentResponse>> GetPagedResponseAsync(SavedContentFilter filter);
        Task<bool> DeleteSavedContentAsync(int id);
    }
}
