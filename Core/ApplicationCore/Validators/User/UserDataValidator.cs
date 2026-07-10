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
                .WithMessage("La contraseña debe tener mínimo 8 caracteres");
        }
    }
}