
namespace RailOps.Entities
{
    public class Section
    {
        public int SectionId { get; set; }

        public string SectionName { get; set; } = string.Empty;

        public int DirectorateId { get; set; }

        public Directorate Directorate { get; set; } = null!;
        public ICollection<WorkTeam> WorkTeams { get; set; } = new List<WorkTeam>();
    }
}
