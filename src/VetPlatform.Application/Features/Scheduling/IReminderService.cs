using VetPlatform.Application.Common;

namespace VetPlatform.Application.Features.Scheduling;

public record ScheduleCareCommand(Guid PetId, string Reason, DateTimeOffset DateTime);
public record ReminderDto(Guid Id, string Subject, string Body, DateTimeOffset? EventAt, string Status);

public interface IReminderService
{
    Task<Result<ReminderDto>> ScheduleCareAsync(ScheduleCareCommand command, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ReminderDto>>> GetMineAsync(CancellationToken ct = default);
    Task<Result<bool>> CancelCareAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> CancelAppointmentAsync(Guid id, CancellationToken ct = default);
}
