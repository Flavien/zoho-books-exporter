using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ZohoBooksExporter
{
    public class TransactionListProcessor
    {
        private readonly ZohoApiClient client;
        private readonly Action<int, int> onProgress;

        public TransactionListProcessor(ZohoApiClient client, Action<int, int> onProgress)
        {
            this.client = client;
            this.onProgress = onProgress;
        }

        public async Task<IReadOnlyList<Transaction>> GetList(string accountId, DateTime from, DateTime to)
        {
            JObject transactionsResult;
            List<JObject> jsonTransactions = new List<JObject>();
            int page = 1;
            do
            {
                transactionsResult = await client.GetTransactions(accountId, from, to, page);

                jsonTransactions.AddRange(((JArray)transactionsResult["banktransactions"]).Cast<JObject>());
                
                page++;

            } while ((bool)transactionsResult["page_context"]["has_more_page"]);

            List<Transaction> result = new List<Transaction>();
            for (int i = 0; i < jsonTransactions.Count; i++)
            {
                result.Add(await ProcessTransaction(jsonTransactions[i]));
                this.onProgress(i + 1, jsonTransactions.Count);
            }

            return result.AsReadOnly();
        }

        private async Task<Transaction> ProcessTransaction(JObject transaction)
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

            return new Transaction()
            {
                Date = DateTime.Parse((string)transaction["date"]).ToString("dd/MM/yyyy"),
                StatementDate = importedTransactions.Count > 0 ? DateTime.Parse((string)importedTransactions[0]["date"]).ToString("dd/MM/yyyy") : "",
                Description = importedTransactions.Count > 0 ? (string)importedTransactions[0]["description"] : (string)transaction["description"],
                Amount = (((string)transaction["debit_or_credit"] == "credit" ? -1m : 1m) * (decimal)transaction["amount"]).ToString(),
                TransactionType = (string)transaction["transaction_type"],
                Account = (string)transaction["account_name"],
                OtherAccount = (string)transaction["offset_account_name"],
                TransactionId = (string)transaction["transaction_id"],
                ImportedTransactionId = (string)transaction["imported_transaction_id"],
                RunningBalance = (string)transaction["running_balance"],
            };
        }
    }
}
