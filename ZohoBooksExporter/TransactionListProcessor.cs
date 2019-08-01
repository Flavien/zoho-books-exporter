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
        private readonly string accountId;
        private readonly Action<int, int> onProgress;

        public TransactionListProcessor(ZohoApiClient client, string accountId, Action<int, int> onProgress)
        {
            this.client = client;
            this.accountId = accountId;
            this.onProgress = onProgress;
        }

        public async Task<IReadOnlyList<Transaction>> GetList(DateTime from, DateTime to)
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
                case "bill":
                    transactionData = JObject.FromObject(new { imported_transactions = new string[0], documents = new string[0] });
                    break;
                default:
                    transactionData = (await client.GetTransaction((string)transaction["transaction_id"]))["banktransaction"];
                    break;
            }

            JToken importedTransaction = ((JArray)transactionData["imported_transactions"])
                .FirstOrDefault(imported => string.Equals((string)imported["account_id"], this.accountId, StringComparison.Ordinal));

            if (importedTransaction == null)
                importedTransaction = ((JArray)transactionData["imported_transactions"]).FirstOrDefault();

            return new Transaction()
            {
                Date = DateTime.Parse((string)transaction["date"]).ToString("dd/MM/yyyy"),
                StatementDate = importedTransaction != null ? DateTime.Parse((string)importedTransaction["date"]).ToString("dd/MM/yyyy") : "",
                Description = importedTransaction != null ? (string)importedTransaction["description"] : (string)transaction["description"],
                Amount = (((string)transaction["debit_or_credit"] == "credit" ? -1m : 1m) * (decimal)transaction["amount"]).ToString(),
                TransactionType = (string)transaction["transaction_type"],
                Account = (string)transaction["account_name"],
                OtherAccount = (string)transaction["offset_account_name"],
                PaidThrough = (string)transactionData["paid_through_account_name"],
                VendorName = (string)transactionData["vendor_name"],
                TransactionId = (string)transaction["transaction_id"],
                ImportedTransactionId = (string)transaction["imported_transaction_id"],
                RunningBalance = (string)transaction["running_balance"],
                Documents = (string)((JArray)transactionData["documents"]).FirstOrDefault()?["file_name"],
            };
        }
    }
}
