using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enginesoft.SyncS3.Service.Models
{
    public class AwsCredential
    {
        public string AcesssKey { get; set; }

        public string SecretKey { get; set; }

        public string Region { get; set; }

        public AwsCredential()
        {

        }

        public AwsCredential(string accessKey, string secretKey, string region)
        {
            this.AcesssKey = accessKey;
            this.SecretKey = secretKey;
            this.Region = region;
        }
    }
}
