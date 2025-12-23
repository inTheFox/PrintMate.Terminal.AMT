using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Comment { get; set; }
        public Guid RoleId { get; set; }

    }
}
