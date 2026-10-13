
using System.ComponentModel.DataAnnotations;

namespace ITSupport.API.DTOs;

public class UpdateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Low|Medium|High|Critical)$")]
    public string Priority { get; set; } = "Medium";

    [Required]
    [RegularExpression("^(Open|In Progress|Pending|Resolved|Closed)$")]
    public string Status { get; set; } = "Open";

    [StringLength(100)]
    public string? Category { get; set; }

    public int? AssignedToUserId { get; set; }
}
