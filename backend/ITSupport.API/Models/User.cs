
using System.ComponentModel.DataAnnotations;

namespace ITSupport.API.Models;

public class User
{
    public int UserId { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    // Stores a password hash, never a plain-text password.
    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Role { get; set; } = "Requester";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
