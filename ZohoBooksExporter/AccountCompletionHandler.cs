namespace ZohoBooksExporter;

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class AccountCompletionHandler : IAutoCompleteHandler
{
    private readonly IReadOnlyList<Account> accounts;

    public char[] Separators { get; set; } = new char[0];

    public AccountCompletionHandler(JObject chartOfAccounts)
    {
        accounts = ((JArray)chartOfAccounts["chartofaccounts"])
            .Select(account =>
                new Account()
                {
                    Name = (string)account["account_name"],
                    Id = (string)account["account_id"]
                })
            .ToList();
    }

    public string[] GetSuggestions(string text, int index)
    {
        return accounts
            .Select(account => account.Name)
            .Where(account => account.Contains(text, StringComparison.InvariantCultureIgnoreCase))
            .ToArray();
    }

    public string GetAccountId(string selection)
    {
        return accounts.FirstOrDefault(account => account.Name.Equals(selection, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private class Account
    {
        public string Name { get; set; }

        public string Id { get; set; }
    }
}
