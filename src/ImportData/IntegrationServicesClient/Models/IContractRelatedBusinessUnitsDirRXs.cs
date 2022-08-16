using System;
using System.Collections.Generic;
using System.Text;

namespace ImportData.IntegrationServicesClient.Models
{
    [EntityName("RelatedBusinessUnitsDirRX")]
    class IContractRelatedBusinessUnitsDirRXs
    {        
        public IBusinessUnits BusinessUnit { get; set; }
        public IContracts Contract { get; set; }
        public IEmployees Employee { get; set; }
    }
}