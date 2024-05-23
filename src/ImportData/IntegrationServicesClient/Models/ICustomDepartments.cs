namespace ImportData.IntegrationServicesClient.Models
{
    [EntityName("Подразделение ERP")]
    public class ICustomDepartments : IEntity
    {
        public ICustomDepartments HeadDepartment { get; set; }
        public string ExternalId { get; set; }
        public string Code { get; set; }
        public string Status { get; set; }
    }
}
