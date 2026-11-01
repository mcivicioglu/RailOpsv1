
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RailOps.ViewModels
{
    public class WorkTeamLeaderAssignViewModel
    {
        public int WorkTeamId { get; set; }

        public string DirectorateName { get; set; }
            = string.Empty;

        public string SectionName { get; set; }
            = string.Empty;

        public string LineGroupName { get; set; }
            = string.Empty;

        public string? TeamLeaderUserId { get; set; }

        public List<SelectListItem> CandidateOptions
        { get; set; } = new();
    }
}
