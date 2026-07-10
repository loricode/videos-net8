using ApplicationCore.Interfaces.Course;
using ApplicationCore.RequestData.Course;
using FluentValidation;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Commands.Course
{
    public class CourseCreate : ICourseCreate
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IValidator<CourseData> _validator;
        public CourseCreate(ICourseRepository courseRepo, IValidator<CourseData> validator)
        {
            _courseRepo = courseRepo;
            _validator = validator;
        }
        public async Task<Guid> SaveAsync(string title, string description, bool state, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken)
        {

            var data = new CourseData { Title = title, Description = description, UserId = userId, StreamFile = stream, FileName = fileName  };

            await _validator.ValidateAndThrowAsync(data, cancellationToken);

            return await _courseRepo.SaveAsync(data.Title, data.Description, state, data.UserId, data.StreamFile, data.FileName, cancellationToken);
        }
    }
}
