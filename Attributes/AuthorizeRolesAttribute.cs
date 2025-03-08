using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace EPinAPI.Attributes
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _roles;
        private const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        public AuthorizeRolesAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userClaims = context.HttpContext.User.Claims.ToList();
            var userRole = userClaims.FirstOrDefault(c => c.Type == RoleClaimType)?.Value;

            if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
            {
                context.Result = new JsonResult(new
                {
                    message = "Bu işlemi gerçekleştirmek için yetkiniz yok!",
                    requiredRoles = _roles,
                    userRole = userRole ?? "Anonim Kullanıcı",
                    //availableClaims = userClaims.Select(c => new { c.Type, c.Value }) // 📌 Debugging için tüm claimleri göster
                })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
