using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enginesoft.SyncS3.Service.Models
{
    public class Configuration
    {
        public List<AwsConfiguration> AwsConfigurations { get; set; }

        public List<SyncConfiguration> SyncConfigurations { get; set; }

        public Configuration()
        {
            this.AwsConfigurations = new List<AwsConfiguration>();
            this.SyncConfigurations = new List<SyncConfiguration>();
        }
    }
}
