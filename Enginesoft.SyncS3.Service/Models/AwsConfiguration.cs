using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enginesoft.SyncS3.Service.Models
{
    public class AwsConfiguration
    {
        public string Name { get; set; }

        public AwsCredential AwsCredential { get; set; }

        public AwsConfiguration()
        {

        }

        public AwsConfiguration(string name, AwsCredential awsCredential)
        {
            this.Name = name;
            this.AwsCredential = awsCredential;
        }
    }
}
