
using Microsoft.AspNetCore.Identity;

namespace RailOps.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;

        public bool MustChangePassword { get; set; } = false;

        public int? WorkTeamId { get; set; }

        public WorkTeam? WorkTeam { get; set; }
        public TechnicalJobTitle? JobTitle { get; set; }

    }
}
