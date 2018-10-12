using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ZohoBooksExporter
{
    class Program
    {
        async static Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configuration = builder.Build();

            ZohoApiClient client = new ZohoApiClient(configuration["api_host"], configuration["api_auth_token"], configuration["organization_id"]);

            Console.Write("Start date [01 Jan 2000]: ");
            string from = Console.ReadLine();

            Console.Write($"End date [{DateTime.UtcNow.ToString("dd MMM yyyy")}]: ");
            string to = Console.ReadLine();

            Console.Write("Account ID: ");
            string accountId = Console.ReadLine();

            DateTime fromDate;
            DateTime toDate;

            if (!DateTime.TryParseExact(from, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out fromDate))
                fromDate = new DateTime(2000, 1, 1);

            if (!DateTime.TryParseExact(to, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out toDate))
                toDate = DateTime.UtcNow;

            await ProcessList(client, accountId, fromDate, toDate);
        }

        private static async Task ProcessList(ZohoApiClient client, string accountId, DateTime from, DateTime to)
        {
            using (TextWriter textWriter = File.CreateText($"Account {accountId} - {from.ToString("dd MMM yyyy")} - {to.ToString("dd MMM yyyy")}.csv"))
            {
                CsvWriter writer = new CsvWriter(textWriter);

                writer.WriteRecord(new
                {
                    Date = "date",
                    StatementDate = "statement_date",
                    Description = "description",
                    Amount = "amount",
                    TransactionType = "transaction_type",
                    Account = "account",
                    OtherAccount = "other_account",
                    TransactionId = "transaction_id",
                    ImportedTransactionId = "statement_id",
                    RunningBalance = "running_balance",
                });

                await writer.NextRecordAsync();

                JObject transactions;
                int page = 1;
                do
                {
                    transactions = await client.GetTransactions(accountId, from, to, page);

                    foreach (JObject transaction in (JArray)transactions["banktransactions"])
                    {
                        await ProcessTransaction(client, writer, transaction);
                    }

                    page++;

                } while ((bool)transactions["page_context"]["has_more_page"]);
            }
        }

        private static async Task ProcessTransaction(ZohoApiClient client, CsvWriter writer, JObject transaction)
        {
            JToken transactionData;
            switch ((string)transaction["transaction_type"])
            {
                case "expense":
                    transactionData = (await client.GetExpense((string)transaction["transaction_id"]))["expense"];
                    break;
                case "vendor_payment":
                    transactionData = (await client.GetVendorPayment((string)transaction["transaction_id"]))["vendorpayment"];
                    break;
                case "journal":
                case "opening_balance":
                case "base_currency_adjustment":
                    transactionData = JObject.FromObject(new { imported_transactions = new string[0] });
                    break;
                default:
                    transactionData = (await client.GetTransaction((string)transaction["transaction_id"]))["banktransaction"];
                    break;
            }

            JArray importedTransactions = (JArray)transactionData["imported_transactions"];

            writer.WriteRecord(new
            {
                Date = DateTime.Parse((string)transaction["date"]).ToString("dd/MM/yyyy"),
                StatementDate = importedTransactions.Count > 0 ? DateTime.Parse((string)importedTransactions[0]["date"]).ToString("dd/MM/yyyy") : "",
                Description = importedTransactions.Count > 0 ? (string)importedTransactions[0]["description"] : (string)transaction["description"],
                Amount = ((string)transaction["debit_or_credit"] == "credit" ? -1m : 1m) * (decimal)transaction["amount"],
                TransactionType = (string)transaction["transaction_type"],
                Account = (string)transaction["account_name"],
                OtherAccount = (string)transaction["offset_account_name"],
                TransactionId = (string)transaction["transaction_id"],
                ImportedTransactionId = (string)transaction["imported_transaction_id"],
                RunningBalance = (string)transaction["running_balance"],
            });

            await writer.NextRecordAsync();
        }
    }
}
