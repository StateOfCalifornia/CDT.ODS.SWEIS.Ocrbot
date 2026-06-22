using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDT.OCRBot.Domain.Models
{
    public class PageData
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public string Unit { get; set; } = string.Empty;
        public List<LineData> Lines { get; set; } = new List<LineData>();
        public List<TableData> Tables { get; set; } = new List<TableData>();
    }

    public class TableData
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<TableCellData> Cells { get; set; } = new List<TableCellData>();
        public List<float> BoundingPolygon { get; set; } = new List<float>();
    }

    public class TableCellData
    {
        public string Content { get; set; } = string.Empty;
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int? RowSpan { get; set; } = 1;
        public int? ColumnSpan { get; set; } = 1;
        public bool IsHeader { get; set; }
        public List<float> BoundingPolygon { get; set; } = new List<float>();
    }
}




