using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onesoftdev.IdentityServer.Models;
using Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Models;

namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Controllers
{
    [SecurityHeaders]
    [Authorize]
    [ApiController]
    [Route("api/users/security")]
    public class AspNetUsersSecurityController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;

        public AspNetUsersSecurityController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [AllowAnonymous]
        [Route("changepassword/{id}", Name = "changepassword")]
        public async Task<IActionResult> ChangePassword(Guid id, AspNetUserPasswordChange passwordChange)
        {
            if (passwordChange == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            // Check that the currentpassword and newpassword values have been supplied.
            if (!string.IsNullOrEmpty(passwordChange.CurrentPassword) &&
                !string.IsNullOrEmpty(passwordChange.NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, passwordChange.CurrentPassword, passwordChange.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new { code = result.Errors.First().Code, error = result.Errors.First().Description });
                }
            }
            else
            {
                ModelState.AddModelError("CurrentPassword or New Password", "Supplied passwords are incorrect or blank");
                return BadRequest();
            }

            return NoContent();
        }

        [AllowAnonymous]
        [Route("confirmemail/{userId}", Name = "confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string securityCode)
        {
            if (userId == null || securityCode == null)
            {
                return BadRequest("User Id or Security Code is null.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, securityCode);
            if (!result.Succeeded)
            {
                //throw new InvalidOperationException($"Error confirming email for user with ID '{userId}':");
                return BadRequest("Invalid token.");
            }

            return Content("Thank you for confirming your email. This page may be closed and you can continue with the mobile application.");
        }

        [AllowAnonymous]
        [Route("confirmphonenumber/{userId}", Name = "confirmphonenumber")]
        public async Task<IActionResult> ConfirmPhoneNumber(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest("User Id or Security Code is null.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var verified = await _userManager.VerifyUserTokenAsync(user, "PhoneNumberToken",
                "Phone number Verification", token);
            if (!verified)
            {
                return BadRequest("Invalid token.");
                //throw new InvalidOperationException($"Error confirming email for user with ID '{userId}':");
            }
            else if (verified)
            {
                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            return Content("Thank you for confirming your mobile number. This page may be closed and you can continue with the mobile application.");
        }
    }
}