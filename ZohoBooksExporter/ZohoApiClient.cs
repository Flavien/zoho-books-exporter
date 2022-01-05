namespace ZohoBooksExporter;

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class ZohoApiClient
{
    private readonly string _host;
    private readonly string _accessToken;
    private readonly string _organizationId;

    public ZohoApiClient(string host, string accessToken, string organizationId)
    {
        _host = host;
        _accessToken = accessToken;
        _organizationId = organizationId;
    }

    public async Task<JObject> GetAccounts()
    {
        return await Get($"chartofaccounts?organization_id={_organizationId}");
    }

    public async Task<JObject> GetTransactions(string account, DateTime from, DateTime to, int page)
    {
        return await Get($"banktransactions?organization_id={_organizationId}&account_id={account}" +
            $"&date_start={from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&date_end={to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&page={page}");
    }

    public async Task<JObject> GetTransaction(string transactionId)
    {
        return await Get($"banktransactions/{transactionId}?organization_id={_organizationId}");
    }

    public async Task<JObject> GetExpense(string transactionId)
    {
        return await Get($"expenses/{transactionId}?organization_id={_organizationId}");
    }

    public async Task<JObject> GetVendorPayment(string transactionId)
    {
        return await Get($"vendorpayments/{transactionId}?organization_id={_organizationId}");
    }

    public async Task<JObject> GetInvoice(string invoiceId)
    {
        return await Get($"invoices/{invoiceId}?organization_id={_organizationId}");
    }

    private async Task<JObject> Get(string url)
    {
        using (HttpClient client = new())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Zoho-authtoken", _accessToken);

            HttpResponseMessage message = await client.GetAsync($"https://{_host}/api/v3/{url}");

            string response = await message.Content.ReadAsStringAsync();
            message.EnsureSuccessStatusCode();

            return JObject.Parse(response);
        }
    }
}
