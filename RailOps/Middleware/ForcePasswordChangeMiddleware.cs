
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using RailOps.Entities;

namespace RailOps.Middleware
{
    public class ForcePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public ForcePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true
                && !IsAllowedRequest(context))
            {
                var user = await userManager
                    .GetUserAsync(context.User);

                if (user?.MustChangePassword == true)
                {
                    context.Response.Redirect(
                        $"{context.Request.PathBase}/Password/Change");

                    return;
                }
            }

            await _next(context);
        }

        private static bool IsAllowedRequest(HttpContext context)
        {
            var path = context.Request.Path;
            var pathText = path.Value ?? string.Empty;

            // Parola değiştirme ekranının GET ve POST işlemleri
            if (pathText.Equals(
                "/Password/Change",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Kullanıcı istediği zaman çıkış yapabilmeli
            if (pathText.Equals(
                "/Identity/Account/Logout",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Görsel ve stil dosyaları
            bool isReadRequest =
                HttpMethods.IsGet(context.Request.Method) ||
                HttpMethods.IsHead(context.Request.Method);

            if (isReadRequest)
            {
                if (path.StartsWithSegments("/css") ||
                    path.StartsWithSegments("/js") ||
                    path.StartsWithSegments("/lib") ||
                    path.StartsWithSegments("/images") ||
                    path.StartsWithSegments("/img") ||
                    path.StartsWithSegments("/assets") ||
                    path.StartsWithSegments("/fonts") ||
                    path.StartsWithSegments("/_framework") ||
                    path.StartsWithSegments("/_content"))
                {
                    return true;
                }

                if (pathText.Equals(
                        "/favicon.ico",
                        StringComparison.OrdinalIgnoreCase) ||
                    pathText.Equals(
                        "/RailOps.styles.css",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
