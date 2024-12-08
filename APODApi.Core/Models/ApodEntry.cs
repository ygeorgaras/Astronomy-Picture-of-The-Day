using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Core.Models
{
    public class ApodEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LocalFilePath { get; set; }
    }
}
