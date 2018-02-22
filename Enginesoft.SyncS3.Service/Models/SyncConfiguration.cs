using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enginesoft.SyncS3.Service.Models
{
    public class SyncConfiguration
    {
        public string Name { get; set; }

        public string AwsConfigurationName { get; set; }

        public string LocalPath { get; set; }

        public string ServerPath { get; set; }

        public string FileFilter { get; set; }

        public SyncConfiguration()
        {

        }

        public SyncConfiguration(string name, string awsConfigurationName, string localPath, string serverPath, string fileFilter)
        {
            this.Name = name;
            this.AwsConfigurationName = awsConfigurationName;
            this.LocalPath = localPath;
            this.ServerPath = serverPath;
            this.FileFilter = fileFilter;
        }
    }
}
