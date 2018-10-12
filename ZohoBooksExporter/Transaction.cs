namespace ZohoBooksExporter
{
    public class Transaction
    {
        public string Date { get; set; }

        public string StatementDate { get; set; }

        public string Description { get; set; }

        public string Amount { get; set; }

        public string TransactionType { get; set; }

        public string Account { get; set; }

        public string OtherAccount { get; set; }

        public string TransactionId { get; set; }

        public string ImportedTransactionId { get; set; }

        public string RunningBalance { get; set; }
    }
}
