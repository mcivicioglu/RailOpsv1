
namespace RailOps.ViewModels
{
    public class WorkTeamLeaderListItemViewModel
    {
        public int WorkTeamId { get; set; }

        public string DirectorateName { get; set; }
            = string.Empty;

        public string SectionName { get; set; }
            = string.Empty;

        public string LineGroupName { get; set; }
            = string.Empty;

        public string? TeamLeaderName { get; set; }
    }
}
