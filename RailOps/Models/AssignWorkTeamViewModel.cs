
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RailOps.ViewModels.Admin
{
    public class AssignWorkTeamViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string UserFullName { get; set; }
            = string.Empty;

        public int? WorkTeamId { get; set; }

        public List<SelectListItem> WorkTeamOptions
        { get; set; } = new();
    }
}
