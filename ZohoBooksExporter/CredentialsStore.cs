namespace ZohoBooksExporter;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CredentialsStore
{
    private static readonly string _credentialsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".zoho-books-exporter");
    private static readonly string _credentialsFilename = "credentials.json";

    public string Get()
    {
        IConfigurationRoot credentials = new ConfigurationBuilder()
            .SetBasePath(_credentialsDirectory)
            .AddJsonFile(_credentialsFilename, optional: true)
            .Build();

        return credentials["oauth:refresh_token"];
    }

    public async Task Set(string refreshToken)
    {
        JObject contents = JObject.FromObject(new
        {
            oauth = new { refresh_token = refreshToken }
        });

        Directory.CreateDirectory(_credentialsDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(_credentialsDirectory, _credentialsFilename),
            contents.ToString(Formatting.Indented));
    }

    public void Clear()
    {
        File.Delete(Path.Combine(_credentialsDirectory, _credentialsFilename));
    }
}
