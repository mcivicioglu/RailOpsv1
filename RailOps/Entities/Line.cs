
namespace RailOps.Entities
{
    public class Line
    {
        public int LineId { get; set; }

        public string LineCode { get; set; } = string.Empty;

        public int LineGroupId { get; set; }

        public LineGroup LineGroup { get; set; } = null!;
    }
}
