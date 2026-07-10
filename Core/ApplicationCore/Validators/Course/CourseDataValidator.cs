using ApplicationCore.RequestData.Course;
using FluentValidation;

namespace ApplicationCore.Validators.Course
{
    public class CourseDataValidator: AbstractValidator<CourseData>
    {
        public CourseDataValidator()
        {

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("El título es obligatorio");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("El userId es obligatorio");
                
        }
    }
}
