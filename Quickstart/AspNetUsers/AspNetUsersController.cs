using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Pages.Account.Internal;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onesoftdev.IdentityServer.Models;
using Onesoftdev.IdentityServer.Quickstart.AspNetUsers.Models;

namespace Onesoftdev.IdentityServer.Quickstart.AspNetUsers
{
    /// <summary>
    /// This controller adds a user to the AspNetUsers table. 
    /// </summary>
    [SecurityHeaders]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AspNetUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;

        public AspNetUsersController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost(Name = "CreateUserByEmail")]
        public async Task<IActionResult> CreateUserByEmail(AspNetUserInput aspNetUserInput)
        {
            if (aspNetUserInput == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check whether 'UsernameType' (AspNetUserNameType) is correct. Should be
            // 'email' or 'mobile' only.
            if (aspNetUserInput.UsernameType != AspNetUserNameType.EMAIL &&
                aspNetUserInput.UsernameType != AspNetUserNameType.PHONE)
            {
                return UnprocessableEntity(new { error = "UserNameType is invalid." });
            }

            var user = new ApplicationUser { UserName = aspNetUserInput.Username };

            // Username input validation. Is the input a valid email or mobile number.
            if (aspNetUserInput.UsernameType == AspNetUserNameType.EMAIL)
            {
                // 1. Validate email address input.
                if (!RegexUtilities.IsValidEmail(aspNetUserInput.Username))
                {
                    ModelState.AddModelError(nameof(aspNetUserInput.UsernameType),
                        "Email address input is invalid.");
                    return BadRequest(ModelState);
                }

                user.Email = aspNetUserInput.Username;
            }

            if (aspNetUserInput.UsernameType == AspNetUserNameType.PHONE)
            {
                // 1. Validate phone number input.
                if (!RegexUtilities.IsValidSAPhoneNumber(aspNetUserInput.Username))
                {
                    ModelState.AddModelError(nameof(aspNetUserInput.UsernameType),
                        "Mobile number input is invalid.");
                    return BadRequest(ModelState);
                }

                user.PhoneNumber = aspNetUserInput.Username;
            }

            if (await _userManager.FindByNameAsync(aspNetUserInput.Username) != null)
                return Conflict($"'{aspNetUserInput.Username}' is already in use.");

            var result = await _userManager.CreateAsync(user, aspNetUserInput.Password);

            if (!result.Succeeded)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    return BadRequest(ModelState);
                }

            if (aspNetUserInput.UsernameType == AspNetUserNameType.EMAIL)
            {
                // Send email confirmation.
            }

            if (aspNetUserInput.UsernameType == AspNetUserNameType.PHONE)
            {
                // Send sms confirmation.
            }

            return CreatedAtRoute("GetUserById", new { user.Id }, user);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("{username}", Name = "GetUserByUsername")]
        public async Task<IActionResult> GetUserByUser(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
    }
}