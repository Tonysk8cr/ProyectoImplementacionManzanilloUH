using FluentValidation;
using VetPlatform.Application.Features.Owners;

namespace VetPlatform.Application.Validators;

public class CreateOwnerProfileCommandValidator : AbstractValidator<CreateOwnerProfileCommand>
{
    public CreateOwnerProfileCommandValidator()
    {
        RuleFor(x => x.IdentificationNumber).NotEmpty().WithMessage("La identificación es obligatoria.")
            .Length(5, 20).WithMessage("La identificación debe tener entre 5 y 20 caracteres.")
            .Matches("^[0-9A-Za-z-]+$").WithMessage("La identificación solo admite letras, números y guiones.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Matches(@"^\+?[0-9\s-]{8,20}$").WithMessage("El teléfono no es válido (mínimo 8 dígitos).");
        RuleFor(x => x.Address).MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres.");
    }
}

public class UpdateOwnerProfileCommandValidator : AbstractValidator<UpdateOwnerProfileCommand>
{
    public UpdateOwnerProfileCommandValidator()
    {
        RuleFor(x => x.IdentificationNumber).NotEmpty().WithMessage("La identificación es obligatoria.")
            .Length(5, 20).WithMessage("La identificación debe tener entre 5 y 20 caracteres.")
            .Matches("^[0-9A-Za-z-]+$").WithMessage("La identificación solo admite letras, números y guiones.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Matches(@"^\+?[0-9\s-]{8,20}$").WithMessage("El teléfono no es válido (mínimo 8 dígitos).");
        RuleFor(x => x.Address).MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres.");
    }
}

/// <summary>Valida solo los datos del propietario; correo, contraseña y nombre los valida el registro de cuentas existente.</summary>
public class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
{
    public RegisterOwnerCommandValidator()
    {
        RuleFor(x => x.IdentificationNumber).NotEmpty().WithMessage("La identificación es obligatoria.")
            .Length(5, 20).WithMessage("La identificación debe tener entre 5 y 20 caracteres.")
            .Matches("^[0-9A-Za-z-]+$").WithMessage("La identificación solo admite letras, números y guiones.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Matches(@"^\+?[0-9\s-]{8,20}$").WithMessage("El teléfono no es válido (mínimo 8 dígitos).");
        RuleFor(x => x.Address).MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres.");
    }
}
