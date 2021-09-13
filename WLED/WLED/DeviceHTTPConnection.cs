using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WLED
{
    class DeviceHttpConnection
    {
        private static DeviceHttpConnection Instance;

        private readonly HttpClient client;

        private DeviceHttpConnection ()
        {
            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
        }
        
        public static DeviceHttpConnection GetInstance()
        {
            return Instance ?? (Instance = new DeviceHttpConnection());
        }

        public async Task<string> Send_WLED_API_Call(string deviceUri, string apiCall)
        {
            try
            {
                string apiCommand = "/win"; //WLED http API URI
                if (!string.IsNullOrEmpty(apiCall))
                {
                    apiCommand += "&";
                    apiCommand += apiCall;
                }
                var result = await client.GetAsync(deviceUri + apiCommand);
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadAsStringAsync();
                } else //404 or other non-success status codes, indicates that target is not WLED device
                {
                    return "err";
                }
            } catch
            {
                return null; //time-out or other connection error
            }
        }
    }
}
