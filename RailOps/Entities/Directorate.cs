
using static System.Collections.Specialized.BitVector32;

namespace RailOps.Entities
{
    public class Directorate
    {
        public int DirectorateId { get; set; }

        public string DirectorateName { get; set; } = string.Empty;

        public ICollection<Section> Sections { get; set; }
            = new List<Section>();
    }
}
