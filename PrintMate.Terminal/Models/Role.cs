using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<string> Permissions { get; set; }

        public string PermissionsDisplay => Permissions != null && Permissions.Count > 0
            ? string.Join(", ", Permissions)
            : "Нет прав";
    }
}
