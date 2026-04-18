using System.ComponentModel.DataAnnotations;

namespace ContentAPI.Models.SavedContent.DTOs
{
    public record CreateSavedContentRequest(
        [StringLength(50)]
        string Title,

        [Required]
        [StringLength(200)]
        string Prompt,

        [StringLength(20)]
        string Tone);
}
