using System;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class ResidenceHistory
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public int ResidentId { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Apartment Apartment { get; set; } = null!;
    public virtual Resident Resident { get; set; } = null!;
}
