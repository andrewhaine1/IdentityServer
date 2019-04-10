using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Pages.Account.Internal;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onesoftdev.IdentityServer.Models;
using Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Helpers;
using Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Models;
using Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Services;
using Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Validation;

namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Controllers
{
    /// <summary>
    /// This controller adds a user to the AspNetUsers table. 
    /// </summary>
    [SecurityHeaders]
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class AspNetUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public AspNetUsersController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger, IEmailService emailService, ISmsService smsService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
            _smsService = smsService;
        }

        [AllowAnonymous]
        [HttpPost(Name = "CreateUser")]
        public async Task<IActionResult> CreateUser(AspNetUserInput aspNetUserInput)
        {
            if (aspNetUserInput == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check whether 'UsernameType' (AspNetUserNameType) is correct. Should be
            // 'email' or 'phone' only.
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
                        "Phone number input is invalid.");
                    return BadRequest(ModelState);
                }

                user.PhoneNumber = aspNetUserInput.Username;
            }

            // Check if the supplied username already exists.
            if (await _userManager.FindByNameAsync(aspNetUserInput.Username) != null)
                return Conflict($"Username: '{aspNetUserInput.Username}' is already in use.");

            var result = await _userManager.CreateAsync(user, aspNetUserInput.Password);

            if (!result.Succeeded)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    return BadRequest(ModelState);
                }

            // Send Confirmation Email or SMS.
            if (aspNetUserInput.UsernameType == AspNetUserNameType.EMAIL)
            {
                // Send email confirmation.
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action("confirmemail", "AspNetUsersSecurity", new { userId = user.Id, securityCode = code },
                    protocol: Request.Scheme);
                try
                {
                    var encodedCallbackUrl = HtmlEncoder.Default.Encode(callbackUrl);
                    _emailService.SendEmail(aspNetUserInput.Username,
                        string.Format("Please confirm your account by clicking on the following link: {0}",
                        encodedCallbackUrl));
                }
                catch(Exception ex)
                {
                    // Log this.
                    _logger.LogError(ex, "Could not send verification email.", null);
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            
            if (aspNetUserInput.UsernameType == AspNetUserNameType.PHONE)
            {
                // Send sms confirmation.
                try
                {
                    var securityToken = await _userManager.GenerateUserTokenAsync(user, "PhoneNumberToken", 
                        "Phone number Verification");
                    await _smsService.SendSms(new SmsService.SmsMessage
                    {
                        Content = securityToken,
                        Destination = aspNetUserInput.Username
                    });
                }
                catch (Exception ex)
                {
                    // Log this.
                    _logger.LogError(ex, "Could not send verification sms.", null);
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            return CreatedAtRoute("GetUser", new { user.Id }, user);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("GetUserByUsername/{username}", Name = "GetUserByUsername")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            if (username == null)
                return BadRequest("Username is null");

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpDelete]
        [Route("{id}", Name = "DeleteUser")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userManager.DeleteAsync(user);

            return NoContent();
        }

        // IDP level changes should not be allowed. Once a user has registered, their email or phone number becomes their username
        // and cannot be changed again. Any changes to email address or phone number need to be done on an application level.

        //[AllowAnonymous]
        //[HttpPut]
        //[Route("{id}", Name = "UpdateUser")]
        //public async Task<IActionResult> UpdateUser(string id, 
        //    [FromBody] AspNetUserUpdate aspNetUserUpdate)
        //{
        //    // Could use automapper to update the AspNetUser entity but manual will do fine :-).
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //        return NotFound();

        //    bool anyChange = false;
        //    bool emailChanged = false;

        //    if (user.Email != aspNetUserUpdate.Email)
        //    {
        //        anyChange = true;
        //        user.Email = aspNetUserUpdate.Email;
        //        emailChanged = true;
        //    }

        //    if (user.PhoneNumber != aspNetUserUpdate.PhoneNumber)
        //    {
        //        anyChange = true;
        //        user.PhoneNumber = aspNetUserUpdate.PhoneNumber;
        //    }

        //    if (!anyChange)
        //        return BadRequest();

        //    await _userManager.UpdateAsync(user);

        //    if (emailChanged)
        //        await _userManager.UpdateNormalizedEmailAsync(user);

        //    return NoContent();
        //}

        //[AllowAnonymous]
        //[HttpPatch]
        //[Route("{id}", Name = "PartiallyUpdateUser")]
        //public async Task<IActionResult> PartiallyUpdateUser(string id, 
        //    [FromBody] JsonPatchDocument<AspNetUserUpdate> patchDoc)
        //{
        //    if (patchDoc == null)
        //        return BadRequest();

        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //        return NotFound();

        //    AspNetUserUpdate aspNetUserUpdate = new AspNetUserUpdate();

        //    patchDoc.ApplyTo(aspNetUserUpdate);
        //    TryValidateModel(aspNetUserUpdate);

        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    bool anyChange = false;
        //    bool emailChanged = false;

        //    if (user.Email != aspNetUserUpdate.Email)
        //    {
        //        anyChange = true;
        //        user.Email = aspNetUserUpdate.Email;
        //        emailChanged = true;
        //    }

        //    if (user.PhoneNumber != aspNetUserUpdate.PhoneNumber)
        //    {
        //        anyChange = true;
        //        user.PhoneNumber = aspNetUserUpdate.PhoneNumber;
        //    }

        //    if (!anyChange)
        //        return BadRequest();

        //    await _userManager.UpdateAsync(user);

        //    if (emailChanged)
        //        await _userManager.UpdateNormalizedEmailAsync(user);

        //    return NoContent();
        //}
    }
}