
namespace RailOps.Entities
{
    public class WorkTeam
    {
        public int WorkTeamId { get; set; }

        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        public int LineGroupId { get; set; }
        public LineGroup LineGroup { get; set; } = null!;
        public string? TeamLeaderUserId { get; set; }

        public ApplicationUser? TeamLeader { get; set; }
    }
}
