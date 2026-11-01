using FluentValidation;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Identity;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Application.Validators;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).Matches("[A-Z]").Matches("[0-9]")
            .WithMessage("La contraseña debe tener al menos 8 caracteres, una mayúscula y un número.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).Must(r => r is not (UserRole.PlatformAdmin or UserRole.ClinicAdmin))
            .WithMessage("Este rol no puede auto-asignarse.");
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreatePetCommandValidator : AbstractValidator<CreatePetCommand>
{
    public CreatePetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Species).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.BirthDate).Must(d => d == null || d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
    }
}

public class RegisterConsultationCommandValidator : AbstractValidator<RegisterConsultationCommand>
{
    public RegisterConsultationCommandValidator()
    {
        RuleFor(x => x.ClinicalRecordId).NotEmpty();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.Instructions).MaximumLength(5000);
        RuleFor(x => x.AttendanceDate).Must(d => d <= DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("La fecha de atención no puede ser futura.");
    }
}

public class RegisterPrescriptionCommandValidator : AbstractValidator<RegisterPrescriptionCommand>
{
    public RegisterPrescriptionCommandValidator()
    {
        RuleFor(x => x.ConsultationId).NotEmpty();
        RuleFor(x => x.Medication).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Route).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 365);
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.ProductId).NotEmpty();
            i.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(1000);
        });
    }
}

public class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.SlotId).NotEmpty();
    }
}
