namespace ImportData.IntegrationServicesClient.Models
{
    [EntityName("Статьи ДДС")]
    public class ITatSpirtPromDdsArticles : IEntity
    {
        public string Code { get; set; }
        public string ExternalSystemId { get; set; }
        public bool? IsGroup { get; set; }
        public string Status { get; set; }
        public ITatSpirtPromDdsArticles MainArticle { get; set; }
    }
}
