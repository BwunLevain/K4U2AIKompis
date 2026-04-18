using System.ComponentModel.DataAnnotations;

namespace ContentAPI.Models.SavedContent.DTOs
{
    public record UpdateSavedContentRequest(
        [StringLength(50)]
        string? Title,

        [StringLength(200)]
        string? Prompt,

        [StringLength(20)]
        string? Tone);
}
