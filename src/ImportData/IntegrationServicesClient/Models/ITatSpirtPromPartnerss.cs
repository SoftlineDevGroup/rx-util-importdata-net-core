namespace ImportData.IntegrationServicesClient.Models
{
    [EntityName("Партнер")]
    public class ITatSpirtPromPartnerss : IEntity
    {
        public string ExtId { get; set; }
        public string BusinessId { get; set; }
        public ITatSpirtPromPartnerss HeadPartner { get; set; }
        public string TIN { get; set; }
        public string TRRC { get; set; }
        public bool RelClient { get; set; }
        public bool RelSupplier { get; set; }
        public bool RelTransporter { get; set; }
        public bool RelByReps { get; set; }
        public bool RelOther { get; set; }
        public string GpLocalCode { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }
    }
}
