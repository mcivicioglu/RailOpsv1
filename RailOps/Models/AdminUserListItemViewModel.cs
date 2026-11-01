
namespace RailOps.ViewModels
{
    public class AdminUserListItemViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string SicilNo { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
