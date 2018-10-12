using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using ShellProgressBar;

namespace ZohoBooksExporter
{
    class Program
    {
        static async Task Main(string[] args)
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

            AccountCompletionHandler chartOfAccounts = new AccountCompletionHandler(await client.GetAccounts());
            ReadLine.AutoCompletionHandler = chartOfAccounts;
            ReadLine.HistoryEnabled = false;
            string accountName = ReadLine.Read("Account: ");
            string accountId = chartOfAccounts.GetAccountId(accountName);

            DateTime fromDate;
            DateTime toDate;

            if (!DateTime.TryParseExact(from, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out fromDate))
                fromDate = new DateTime(2000, 1, 1);

            if (!DateTime.TryParseExact(to, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out toDate))
                toDate = DateTime.UtcNow;

            Console.WriteLine();

            ProgressBarOptions options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                BackgroundCharacter = '\u2593'
            };
            using (ProgressBar progressBar = new ProgressBar(1, "Initial message", options))
            {
                TransactionListProcessor listProcessor = new TransactionListProcessor(client, (current, total) => UpdateProgress(progressBar, current, total));
                IReadOnlyList<Transaction> transactions = await listProcessor.GetList(accountId, fromDate, toDate);

                await WriteCsv($"{accountName} - From {fromDate.ToString("dd MMM yyyy")} to {toDate.ToString("dd MMM yyyy")}.csv", transactions);
            }
        }

        private static void UpdateProgress(ProgressBar progressBar, int current, int total)
        {
            progressBar.MaxTicks = total;
            progressBar.Tick($"   Downloading transaction {current} out of {total}");
        }

        private static async Task WriteCsv(string fileName, IReadOnlyList<Transaction> transactions)
        {
            using (TextWriter textWriter = File.CreateText(fileName))
            {
                CsvWriter writer = new CsvWriter(textWriter);

                writer.WriteRecord(new Transaction
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

                writer.WriteRecords(transactions);
            }
        }
    }
}
