using ContentAPI.Models.SavedContent;

namespace ContentAPI.Services
{
    public class InMemorySavedContentRepository : ISavedContentRepository
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, SavedContent> _store;

        public InMemorySavedContentRepository()
        {
            _store = new(System.Linq.Enumerable.Range(1, 10).Select(i => new KeyValuePair<int, SavedContent>(i, new SavedContent
            {
                Id = i,
                Title = $"Saved Content {i}",
                Prompt = $"This is the prompt for saved content {i}.",
                Content = $"Initial AI Content {i}.",
                Tone = i % 2 == 0 ? "Professional" : "Informative",
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow.AddDays(-i / 2)
            })));
        }

        public Task AddAsync(SavedContent entity)
        {
            _store[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(int id)
        {
            return Task.FromResult(_store.TryRemove(id, out _));
        }

        public Task<IEnumerable<SavedContent>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<SavedContent>>(_store.Values.ToList());
        }

        public Task<SavedContent?> GetByIdAsync(int id)
        {
            _store.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }

        public Task<bool> UpdateAsync(SavedContent entity)
        {
            _store[entity.Id] = entity;
            return Task.FromResult(true);
        }
    }
}