namespace ContentAPI.Models.SavedContent.DTOs
{
    public record SavedContentResponse(
        int Id,
        string Title,
        string Prompt,
        string Content,
        string Headline,
        string[] Paragraphs,
        bool UncertaintyFlag,
        string Tone,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
