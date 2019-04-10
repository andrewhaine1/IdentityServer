namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Services
{
    public interface IEmailService
    {
        void SendEmail(string recipientEmail, string htmlMessage);
    }
}
