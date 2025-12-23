using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrintMate.Terminal.AppConfiguration;
using Permissions = PrintMate.Terminal.Models.Permissions;

namespace PrintMate.Terminal.Services
{
    public class PermissionManagerService
    {
        private readonly RolesService _rolesService;
        private readonly AuthorizationService _authorizationService;

        public PermissionManagerService(RolesService rolesService, AuthorizationService authorizationService)
        {
            _rolesService = rolesService;
            _authorizationService = authorizationService;
        }

        public bool HasPermission(Permission permission)
        {
            if (_authorizationService.GetUser() == null) return false;
            if (_authorizationService.IsRootAuthorized()) return true;

            var user = _authorizationService.GetUser();
            var role = _rolesService.GetRoleById(user.RoleId);
            if (role == null) return false;
            if (role.Permissions.Contains(permission.Id)) return true;
            return false;
        }
    }
}
