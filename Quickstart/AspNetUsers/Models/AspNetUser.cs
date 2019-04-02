using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onesoftdev.IdentityServer.Quickstart.AspNetUsers.Models
{
    public class AspNetUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Phone { get; set; }
        public bool PhoneConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}
