using Microsoft.EntityFrameworkCore;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.AppConfiguration
{
    public class RoleSettings : ConfigurationModelBase
    {
        public List<Role> Roles  = new List<Role>();
    }
}
