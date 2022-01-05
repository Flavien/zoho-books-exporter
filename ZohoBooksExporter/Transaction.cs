namespace ZohoBooksExporter;

public record Transaction
{
    public string Date { get; init; }

    public string StatementDate { get; init; }

    public string Description { get; init; }

    public string Amount { get; init; }

    public string TransactionType { get; init; }

    public string Account { get; init; }

    public string OtherAccount { get; init; }

    public string PaidThrough { get; init; }

    public string VendorName { get; init; }

    public string TransactionId { get; init; }

    public string ImportedTransactionId { get; init; }

    public string RunningBalance { get; init; }

    public string Documents { get; init; }
}
