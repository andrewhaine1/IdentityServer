using System.Threading.Tasks;
using static Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Services.SmsService;

namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Services
{
    public interface ISmsService
    {
        Task<bool> SendSms(SmsMessage smsMessage);
    }
}
