namespace ZohoBooksExporter;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Quickwire.Attributes;

[RegisterService(ServiceLifetime.Singleton)]
public class OAuthClient
{
    private readonly string _domain;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public OAuthClient(string domain, string clientId, string clientSecret)
    {
        _domain = domain;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public async Task<string> GetAccessToken(string code)
    {
        Uri uri = new(
            $"https://{_domain}/oauth/v2/token" +
            $"?client_id={HttpUtility.UrlEncode(_clientId)}" +
            $"&client_secret={HttpUtility.UrlEncode(_clientSecret)}" +
            $"&code={HttpUtility.UrlEncode(code)}" +
            $"&grant_type=authorization_code" +
            $"&redirect_uri={HttpUtility.UrlEncode("http://localhost/oauth")}");

        using (HttpClient client = new())
        {
            HttpResponseMessage response = await client.PostAsync(uri, new StringContent(""));

            string sringResult = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            JObject json = JObject.Parse(sringResult);

            return (string)json["access_token"];
        }
    }
}
