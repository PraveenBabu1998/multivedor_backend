namespace elemechWisetrack.Models
{
    public class VandorBankDetail
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }

        public string? BankType { get; set; }
        public string IFSCCode { get; set; }
        public string BranchName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
