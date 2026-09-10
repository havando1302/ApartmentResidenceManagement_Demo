using System;
using System.Collections.Generic;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class Resident
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public GenderType Gender { get; set; }
    public string? IdentityCard { get; set; }
    public string? PhoneNumber { get; set; }
    public string? HomeTown { get; set; }

    // Navigation properties
    public virtual ICollection<ResidenceHistory> ResidenceHistories { get; set; } = new List<ResidenceHistory>();
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public virtual UserAccount? UserAccount { get; set; }
}
