using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCore.DTOs.Video
{
    public class VideoDto
    {

        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Status {  get; set; } = string.Empty;

        public string StreamUrl { get; set; } = string.Empty;

        public DateTime CreateAt { get; set; }

    }
}
