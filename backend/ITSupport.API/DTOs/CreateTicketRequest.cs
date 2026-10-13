
using System.ComponentModel.DataAnnotations;

namespace ITSupport.API.DTOs;

public class CreateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int CreatedByUserId { get; set; }

    public int? AssignedToUserId { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [RegularExpression("^(Low|Medium|High|Critical)$")]
    public string Priority { get; set; } = "Medium";
}
