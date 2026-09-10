using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int? ResidentId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Resident? Resident { get; set; }
}
