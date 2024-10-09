using System;

namespace ImportData.IntegrationServicesClient.Models
{
    [EntityName("Договор")]
    public class IContracts : IContractualDocuments
    {
        public ITatSpirtPromDdsArticles DdsArticlesline { get; set; }
        public IVatRates VATRatesline { get; set; }
        public double VATAmountsline { get; set; }
        public string AccountDetailssline { get; set; }
        public ICurrencies PaymentCurrencysline { get; set; }
        public ICustomDepartments ERPDepartmentsline { get; set; }
        
        public ITatSpirtPromPartnerss Partnersline { get; set; }
    }


}
