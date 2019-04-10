using System.ComponentModel.DataAnnotations;

namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Models
{
    public class AspNetUserInput
    {
        [Required]
        public string UsernameType { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        //[DataType(DataType.Password)]
        //[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        //public string ConfirmPassword { get; set; }
    }

}
