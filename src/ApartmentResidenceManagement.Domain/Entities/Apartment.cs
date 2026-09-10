using System.Collections.Generic;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class Apartment
{
    public int Id { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public double Area { get; set; }
    public ApartmentStatus Status { get; set; } = ApartmentStatus.Empty;

    // Navigation properties
    public virtual ICollection<ResidenceHistory> ResidenceHistories { get; set; } = new List<ResidenceHistory>();
}
