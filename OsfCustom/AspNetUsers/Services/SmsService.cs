using System;
using System.Net;
using System.Web;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Onesoftdev.IdentityServer.OsfCustom.AspNetUsers.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        private readonly Task<AuthResponse> _authResponseTask;
        private DateTime _authTokenExpitesAt;

        public SmsService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = new HttpClient();
            _authResponseTask = GetAuthResponseAsync();
        }

        async Task<AuthResponse> GetAuthResponseAsync()
        {
            // Get ClientId and ClientSecret from appsettings file.
            var clientId = _configuration["SmsPortal:ClientId"];
            var clientSecret = _configuration["SmsPortal:ClientSecret"];

            var authUri = _configuration["SmsPortal:AuthEndpoint"];

            // URL Encode ClientId and ClientSecret. Not sure why :-).
            var clientIdUrlEncoded = HttpUtility.UrlEncode(clientId);
            var clientSecretUrlEncoded = HttpUtility.UrlEncode(clientSecret);

            // Combine ClientId and ClientSecret.
            var clientIdAndSecret = string.Format("{0}:{1}", clientIdUrlEncoded, clientSecretUrlEncoded);

            // Convert ClientId and secret to byte array.
            byte[] data = System.Text.Encoding.ASCII.GetBytes(clientIdAndSecret);

            // Convert Client Id and secret byte array to Base64 encoded string. This is what Postman does when
            // the Authorization type is set to Basic Auth and a username and password are supplied, it will combine the username
            // and password and base64 encode it and place it in the request header in as the value in the Authorization field.
            var authorizationHeaderValue = Convert.ToBase64String(data);

            // Create Http Request Message/Object.
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(authUri),
                Content = new StringContent("")
            };

            // Set Http Request Content-Type.
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json") { CharSet = "UTF-8" };

            // Add authorization parameter and value to the header.
            httpRequestMessage.Headers.TryAddWithoutValidation("Authorization", string.Format("Basic {0}", authorizationHeaderValue));

            var httpResponse = await _httpClient.SendAsync(httpRequestMessage);
            var httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            var tokenResponse = JObject.Parse(httpResponseContent).ToObject<AuthResponse>();

            if (tokenResponse.ExpiresInMinutes != null)
                _authTokenExpitesAt = double.TryParse(tokenResponse.ExpiresInMinutes, out double expiresAt) ?
                    DateTime.Now.AddMinutes(expiresAt) : DateTime.Now; 

            return tokenResponse;
        }

        public async Task<bool> SendSms(SmsMessage smsMessage)
        {
            // If the Auth token has expired, get it again.
            if (_authTokenExpitesAt != null && _authTokenExpitesAt > DateTime.Now)
                await GetAuthResponseAsync();

            var sendSmdUri = _configuration["SmsPortal:SmsMessageEndpoint"];

            // Get authResopnse Object from SmsPortal
            var authResponse = await _authResponseTask; 
            var messages = new SmsMessage[] { smsMessage };
            var jsonContent = JsonConvert.SerializeObject(new { Messages = messages });

            var stringContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Create Http Request Message/Object.
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(sendSmdUri),
                Content = stringContent
            };

            // Set Http Request Content-Type.
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json") { CharSet = "UTF-8" };

            // Add authorization parameter and value to the header.
            httpRequestMessage.Headers.TryAddWithoutValidation("Authorization", string.Format("Basic {0}", authResponse.Token));

            var httpResponse = await _httpClient.SendAsync(httpRequestMessage);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
                return true;

            return false;
        }

        public class SmsMessage
        {
            public string Content { get; set; }
            public string Destination { get; set; }
        }

        public class AuthResponse
        {
            public string Token { get; set; }
            public string Schema { get; set; }
            public string ExpiresInMinutes { get; set; }
        }
    }
}
