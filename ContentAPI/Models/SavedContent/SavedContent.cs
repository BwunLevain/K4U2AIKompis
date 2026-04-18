namespace ContentAPI.Models.SavedContent
{
    public class SavedContent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Tone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
