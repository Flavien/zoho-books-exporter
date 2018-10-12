using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ZohoBooksExporter
{
    public class ZohoApiClient
    {
        private readonly string host;
        private readonly string accessToken;
        private readonly string organizationId;

        public ZohoApiClient(string host, string accessToken, string organizationId)
        {
            this.host = host;
            this.accessToken = accessToken;
            this.organizationId = organizationId;
        }

        public async Task<JObject> GetTransactions(string account, DateTime from, DateTime to, int page)
        {
            return await Get($"banktransactions?organization_id={this.organizationId}&account_id={account}" +
                $"&date_start={from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&date_end={to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&page={page}");
        }

        public async Task<JObject> GetTransaction(string transactionId)
        {
            return await Get($"banktransactions/{transactionId}?organization_id={this.organizationId}");
        }

        public async Task<JObject> GetExpense(string transactionId)
        {
            return await Get($"expenses/{transactionId}?organization_id={this.organizationId}");
        }

        public async Task<JObject> GetVendorPayment(string transactionId)
        {
            return await Get($"vendorpayments/{transactionId}?organization_id={this.organizationId}");
        }

        private async Task<JObject> Get(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Zoho-authtoken", this.accessToken);

                HttpResponseMessage message = await client.GetAsync($"https://{this.host}/api/v3/{url}");

                string response = await message.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

                return JObject.Parse(response);
            }
        }
    }
}
