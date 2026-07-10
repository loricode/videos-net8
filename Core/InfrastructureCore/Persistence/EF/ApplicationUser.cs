using System;
using System.Collections.Generic;
using System.Text;
using InfrastructureCore.Persistence.EF.Entities;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureCore.Persistence.EF
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;

        public ICollection<CourseEntity> Courses { get; set; } = new List<CourseEntity>();
    }
}
