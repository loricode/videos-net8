using DomainCore.RequestData.User;
using FluentValidation;

namespace ApplicationCore.Validators.User
{
    public class UserDataValidator : AbstractValidator<UserData>
    {
        public UserDataValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("El nombre es obligatorio");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("El correo es obligatorio")
                .EmailAddress()
                .WithMessage("El correo no tiene un formato válido");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("La contraseña es obligatoria")
                .MinimumLength(8)
                .WithMessage("La contraseña debe tener mínimo 8 caracteres")
                .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una mayúscula")
                .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número")
                .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe tener al menos un símbolo");

            RuleFor(x => x.FullName)
                .MaximumLength(120)
                .WithMessage("El nombre admite máximo 120 caracteres");
        }
    }
}