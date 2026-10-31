using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Emergency;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Emergency;
using VetPlatform.Domain.Entities.Marketplace;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;

namespace VetPlatform.Infrastructure.Services;

internal static class Mapping
{
    public static PetDto ToDto(this Pet p) => new(p.Id, p.OwnerId, p.Name, p.Species, p.Breed, p.BirthDate, p.QrCode, p.CreatedAt);
    public static AddendumDto ToDto(this Addendum a) => new(a.Id, a.AuthorUserId, a.Reason, a.Content, a.CreatedAt);
    public static AddendumDto ToDto(this PrescriptionAddendum a) => new(a.Id, a.AuthorUserId, a.Reason, a.Content, a.CreatedAt);
    public static PrescriptionDto ToDto(this Prescription p) => new(p.Id, p.ConsultationId, p.VetUserId, p.Medication, p.Dosage, p.Route,
        p.Frequency, p.DurationDays, p.CreatedAt, p.Addenda.OrderBy(a => a.CreatedAt).Select(a => a.ToDto()).ToList());
    public static ConsultationDto ToDto(this Consultation c) => new(c.Id, c.ClinicalRecordId, c.VetUserId, c.AttendanceDate, c.RegistrationDate,
        c.Notes, c.Instructions, c.Status.ToString(), c.SourceType, c.Addenda.OrderBy(a => a.CreatedAt).Select(a => a.ToDto()).ToList(),
        c.Prescriptions.OrderBy(p => p.CreatedAt).Select(p => p.ToDto()).ToList());
    public static EmergencyAccessDto ToDto(this EmergencyAccess a) => new(a.Id, a.PetId, a.VetUserId, a.Reason, a.GrantedAt, a.ExpiresAt, a.Status.ToString());
    public static EmergencyAttendanceDto ToDto(this EmergencyAttendance a) => new(a.Id, a.EmergencyAccessId, a.ClinicalRecordId, a.VetUserId,
        a.AttendanceDate, a.RegistrationDate, a.Findings, a.Actions, a.Instructions);
    public static ClinicDto ToDto(this Clinic c) => new(c.Id, c.Name, c.Status.ToString(), c.RepresentativeUserId, c.Address, c.Phone, c.CreatedAt,
        c.Members.Select(m => m.ToDto()).ToList());
    public static ClinicMemberDto ToDto(this ClinicMember m) => new(m.Id, m.UserId, m.JoinedAt, m.LeftAt);
    public static AttendingVetRelationshipDto ToDto(this AttendingVetRelationship r) => new(r.Id, r.PetId, r.VetUserId, r.ClinicId, r.EstablishedAt, r.RevokedAt, r.IsActive);
    public static StoreDto ToDto(this Store s) => new(s.Id, s.Name, s.Status.ToString(), s.OperatorUserId, s.Address);
    public static ProductDto ToDto(this Product p) => new(p.Id, p.StoreId, p.Name, p.Description, p.Price, p.IsPublished, p.AvailableStock);
    public static OrderDto ToDto(this Order o) => new(o.Id, o.BuyerUserId, o.StoreId, o.Status.ToString(), o.CreatedAt, o.UpdatedAt, o.Notes, o.Total,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.ReservedQuantity)).ToList());
    public static AvailabilitySlotDto ToDto(this AvailabilitySlot s) => new(s.Id, s.ClinicId, s.VetUserId, s.StartTime, s.EndTime, s.IsBooked);
    public static AppointmentDto ToDto(this Appointment a) => new(a.Id, a.PetId, a.OwnerId, a.ClinicId, a.VetUserId, a.SlotId, a.CreatedAt, a.Status);
}
