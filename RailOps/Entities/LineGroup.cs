
namespace RailOps.Entities
{
    public class LineGroup
    {
        public int LineGroupId { get; set; }

        public string LineGroupName { get; set; } = string.Empty;

        public ICollection<Line> Lines { get; set; } = new List<Line>();
        public ICollection<WorkTeam> WorkTeams { get; set; } = new List<WorkTeam>();
    }
}
