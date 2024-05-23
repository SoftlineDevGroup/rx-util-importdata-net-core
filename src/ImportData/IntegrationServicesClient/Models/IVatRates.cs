namespace ImportData.IntegrationServicesClient.Models
{
    [EntityName("Ставки НДС")]
    public class IVatRates : IEntity
    {
        public int Rate { get; set; }
        public string Status { get; set; }
    }
}
