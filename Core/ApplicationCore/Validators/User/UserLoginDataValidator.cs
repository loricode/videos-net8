using ApplicationCore.RequestData.User;
using FluentValidation;

namespace ApplicationCore.Validators.User
{
    public class UserLoginDataValidator : AbstractValidator<UserLoginData>
    {

        public UserLoginDataValidator()
        {

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
