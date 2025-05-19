using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.Models
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base() {}
        public ApplicationRole(string roleName) : base(roleName) { }

        public string Description { get; set; }

        public virtual ICollection<ApplicationUser> UserRoles { get; set; }
    }
}
