using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDT.OCRBot.Domain.Models
{
    public class LineData
    {
        public string Content { get; set; } = string.Empty;
        public List<float> BoundingPolygon { get; set; } = new List<float>();
    }
}




