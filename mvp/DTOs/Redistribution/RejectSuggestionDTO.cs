using System.ComponentModel.DataAnnotations;

namespace mvp.DTOs.Redistribution
{
    public class RejectSuggestionDTO
    {
        [Required]
        [StringLength(500, MinimumLength = 3)]
        public string Reason { get; set; } = null!;
    }
}
