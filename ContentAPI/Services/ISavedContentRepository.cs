using ContentAPI.Models.SavedContent;

namespace ContentAPI.Services
{
    public interface ISavedContentRepository
    {
        Task<SavedContent?> GetByIdAsync(int id);
        Task<IEnumerable<SavedContent>> GetAllAsync();
        Task AddAsync(SavedContent entity);
        Task<bool> UpdateAsync(SavedContent entity);
        Task<bool> DeleteAsync(int id);
    }
}