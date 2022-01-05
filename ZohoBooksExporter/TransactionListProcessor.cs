namespace ZohoBooksExporter;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class TransactionListProcessor
{
    private readonly ZohoApiClient _client;
    private readonly string _accountId;
    private readonly Action<int, int> _onProgress;

    public TransactionListProcessor(ZohoApiClient client, string accountId, Action<int, int> onProgress)
    {
        _client = client;
        _accountId = accountId;
        _onProgress = onProgress;
    }

    public async Task<IReadOnlyList<Transaction>> GetList(DateTime from, DateTime to)
    {
        JObject transactionsResult;
        List<JObject> jsonTransactions = new();
        int page = 1;
        do
        {
            transactionsResult = await _client.GetTransactions(_accountId, from, to, page);

            jsonTransactions.AddRange(((JArray)transactionsResult["banktransactions"]).Cast<JObject>());

            page++;

        } while ((bool)transactionsResult["page_context"]["has_more_page"]);

        List<Transaction> result = new();
        for (int i = 0; i < jsonTransactions.Count; i++)
        {
            result.Add(await ProcessTransaction(jsonTransactions[i]));
            _onProgress(i + 1, jsonTransactions.Count);
        }

        return result.AsReadOnly();
    }

    private async Task<Transaction> ProcessTransaction(JObject transaction)
    {
        JToken transactionData;
        switch ((string)transaction["transaction_type"])
        {
            case "expense":
                transactionData = (await _client.GetExpense((string)transaction["transaction_id"]))["expense"];
                break;
            case "vendor_payment":
                transactionData = (await _client.GetVendorPayment((string)transaction["transaction_id"]))["vendorpayment"];
                break;
            case "journal":
            case "opening_balance":
            case "base_currency_adjustment":
            case "bill":
                transactionData = JObject.FromObject(new { imported_transactions = new string[0], documents = new string[0] });
                break;
            case "invoice":
                transactionData = (await _client.GetInvoice((string)transaction["transaction_id"]))["invoice"];
                break;
            default:
                transactionData = (await _client.GetTransaction((string)transaction["transaction_id"]))["banktransaction"];
                break;
        }

        JToken importedTransaction = ((JArray)transactionData["imported_transactions"])
            ?.FirstOrDefault(imported => string.Equals((string)imported["account_id"], _accountId, StringComparison.Ordinal));

        if (importedTransaction == null)
            importedTransaction = ((JArray)transactionData["imported_transactions"])?.FirstOrDefault();

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
            VendorName = (string)transactionData["vendor_name"] ?? (string)transactionData["customer_name"],
            TransactionId = (string)transaction["transaction_id"],
            ImportedTransactionId = (string)transaction["imported_transaction_id"],
            RunningBalance = (string)transaction["running_balance"],
            Documents = (string)((JArray)transactionData["documents"]).FirstOrDefault()?["file_name"],
        };
    }
}
