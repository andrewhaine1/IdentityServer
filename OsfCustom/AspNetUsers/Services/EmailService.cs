using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;

namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string recipientEmail, string htmlMessage)
        {
            string smtpHost = _configuration["SmtpSettings:Host"];
            string idpEmailVerificationAccount = _configuration["SmtpSettings:EmailAddress"];
            string idpEmailVerificationAccountPassword = _configuration["SmtpSettings:Password"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Onesoft Development IDP", idpEmailVerificationAccount));
            message.To.Add(new MailboxAddress(recipientEmail));
            message.Subject = "Onesoft Development IDP Email Address Verifcation";

            message.Body = new TextPart("plain")
            {
                Text = htmlMessage
            };

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect(smtpHost, 587, false);

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(idpEmailVerificationAccount, idpEmailVerificationAccountPassword);

                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}
