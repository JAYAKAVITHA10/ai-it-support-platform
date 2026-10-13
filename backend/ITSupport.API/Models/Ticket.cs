
using System.ComponentModel.DataAnnotations;

namespace ITSupport.API.Models;

public class Ticket
{
    public int TicketId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Category { get; set; } = "Unclassified";

    [Required, StringLength(20)]
    public string Priority { get; set; } = "Medium";

    [Required, StringLength(30)]
    public string Status { get; set; } = "Open";

    public string? AISummary { get; set; }

    [StringLength(20)]
    public string? AIPredictedPriority { get; set; }

    public DateTime? SLADueAt { get; set; }

    public int CreatedByUserId { get; set; }

    public int? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
