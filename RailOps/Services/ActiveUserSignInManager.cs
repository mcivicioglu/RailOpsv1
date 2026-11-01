
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailOps.Entities;

namespace RailOps.Services
{
    public class ActiveUserSignInManager
        : SignInManager<ApplicationUser>
    {
        public ActiveUserSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(
                userManager,
                contextAccessor,
                claimsFactory,
                optionsAccessor,
                logger,
                schemes,
                confirmation)
        {
        }

        public override async Task<bool> CanSignInAsync(
            ApplicationUser user)
        {
            if (!user.IsActive)
                return false;

            return await base.CanSignInAsync(user);
        }
    }
}
