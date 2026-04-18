namespace ContentAPI.Models.SavedContent.DTOs
{
    public record SavedContentFilter(
        DateTime? CreatedAt = null,
        DateTime? UpdatedAt = null,
        string? Tone = null,
        string? Sort = null,
        int Page = 1,
        int PageSize = 10);
}