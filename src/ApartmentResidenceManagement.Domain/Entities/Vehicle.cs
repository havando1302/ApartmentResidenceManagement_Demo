using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string? Brand { get; set; }
    public int OwnerId { get; set; }
    public VehicleRegistrationStatus RegistrationStatus { get; set; } = VehicleRegistrationStatus.Pending;

    // Navigation properties
    public virtual Resident Owner { get; set; } = null!;
}
