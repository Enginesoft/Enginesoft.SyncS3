using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Enginesoft.SyncS3.Service
{
    public partial class EntryService : ServiceBase
    {
        private System.IO.FileSystemWatcher configurationFileSystemWatcher = null;
        private System.Timers.Timer configurationTimer = null;
        private Models.Configuration configuration = null;
        
        private SyncActivity[] syncActivitiesArray = new SyncActivity[0];

        public EntryService()
        {
            InitializeComponent();

            configurationTimer = new System.Timers.Timer();
            configurationTimer.Interval = 1000;
            configurationTimer.Enabled = false;
            configurationTimer.Elapsed += ConfigurationTimer_Elapsed;
        }
        
        public void Start()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                ServiceUtils.ClearLog();

            ServiceUtils.WriteLog("Starting service...");
            LoadConfiguration();
            StartSync();
            StartMonitoringConfigurationChanges();
        }

        public new void Stop()
        {
            StopMonitoringConfigurationChanges();
            ServiceUtils.WriteLog("Service Stopped");
        }
        
        #region Configuration

        private void LoadConfiguration()
        {
            ServiceUtils.WriteLog("Loading configuration file...");
            this.configuration = GetConfiguration();
        }

        private Models.Configuration GetConfiguration()
        {
            string path = ServiceUtils.GetConfigurationPath();
            if (!System.IO.File.Exists(path))
            {
                ServiceUtils.WriteLog(string.Format("ERROR! Configuration file not found. Path: {0}", path));

                System.Diagnostics.Debugger.Break();
                //There is no configuration file in this solution, follow the instructions below to create a new:
                //1- create a file "main.json" in solution folder "Config" using "main.json.example" as template
                //2- Select in solution the created file "main.json", press F4 to go to "properties" and change the property "Copy to output directory" to "Copy Always"
                //3- Run the project again

                return null;
            }

            Models.Configuration configuration = null;

            try
            {
                var json = GetConfigurationJson(path);
                var jsonSeriallizerSettings = GetJsonSeriallizerSettings();
                configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Configuration>(json, jsonSeriallizerSettings);
            }
            catch (System.Exception ex)
            {
                ServiceUtils.WriteLog(string.Format("ERROR! Configuration file is invalid -- {0}", ex.Message));
                return null;
            }

            if (!ValidateConfiguration(configuration))
                return null;

            ServiceUtils.WriteLog(string.Format("Configuration file loaded -- {0} awsConfiguration(s), {1} syncConfiguration(s)", configuration.AwsConfigurations.Count, configuration.SyncConfigurations.Count));

            for (int i=0; i<configuration.SyncConfigurations.Count; i++)
                ServiceUtils.WriteLog(string.Format("Configuration file -- SyncConfiguration[{0}] = {{name: \"{1}\", localPath: \"{2}\", serverPath: \"{3}\"}}", i, configuration.SyncConfigurations[i].Name, configuration.SyncConfigurations[i].LocalPath, configuration.SyncConfigurations[i].ServerPath));
            
            return configuration;
        }

        private bool ValidateConfiguration(Models.Configuration configuration)
        {
            //check awsConfiguration name duplication

            var dupplicateAwsConfigurationNames =
                (from c in configuration.AwsConfigurations
                 group c by c.Name into grp
                 where grp.Count() > 1
                 select grp.Key).ToList();

            if (dupplicateAwsConfigurationNames.Count > 0)
            {
                ServiceUtils.WriteLog(string.Format("ERROR! Configuration file is invalid. AwsConfiguration name=\"{0}\" is duplicate", dupplicateAwsConfigurationNames[0]));
                return false;
            }

            //check syncConfiguration name duplication

            var dupplicateSyncConfigurationNames =
                (from c in configuration.SyncConfigurations
                 group c by c.Name into grp
                 where grp.Count() > 1
                 select grp.Key).ToList();

            if (dupplicateSyncConfigurationNames.Count > 0)
            {
                ServiceUtils.WriteLog(string.Format("ERROR! Configuration file is invalid. SyncConfiguration name=\"{0}\" is duplicate", dupplicateSyncConfigurationNames[0]));
                return false;
            }
            
            foreach (var syncConfiguration in configuration.SyncConfigurations)
            {
                if (string.IsNullOrEmpty(syncConfiguration.Name))
                {
                    ServiceUtils.WriteLog("ERROR! SyncConfiguration Name is required");
                    return false;
                }

                if (string.IsNullOrEmpty(syncConfiguration.LocalPath))
                {
                    ServiceUtils.WriteLog(string.Format("ERROR! SyncConfiguration: \"{0}\" property \"LocalPath\" is required"));
                    return false;
                }

                if (!System.IO.Directory.Exists(syncConfiguration.LocalPath))
                {
                    ServiceUtils.WriteLog(string.Format("ERROR! SyncConfiguration: \"{0}\" property \"LocalPath\" is invalid, directory not found"));
                    return false;
                }

                //check awsConfigurationName in syncConfiguration
                var awsConfiguration = configuration.AwsConfigurations.Where(a => string.Equals(a.Name, syncConfiguration.AwsConfigurationName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                if (awsConfiguration == null)
                {
                    ServiceUtils.WriteLog(string.Format("ERROR! awsConfigurationName \"{0}\" filled in syncConfiguration \"{1}\" is invalid", syncConfiguration.AwsConfigurationName, syncConfiguration.Name));
                    return false;
                }

                //check localPath in syncConfiguration
                if (!System.IO.Directory.Exists(syncConfiguration.LocalPath))
                {
                    ServiceUtils.WriteLog(string.Format("ERROR! localPath \"{0}\" invalid, path not found", syncConfiguration.LocalPath));
                    return false;
                }
            }

            return true;
        }

        #region Monitor Configuration file changes

        private void StartMonitoringConfigurationChanges()
        {
            var configurationPath = ServiceUtils.GetConfigurationPath();

            configurationFileSystemWatcher = new System.IO.FileSystemWatcher();
            configurationFileSystemWatcher.Path = System.IO.Path.GetDirectoryName(configurationPath);
            configurationFileSystemWatcher.Filter = System.IO.Path.GetFileName(configurationPath);
            configurationFileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            configurationFileSystemWatcher.Changed += ConfigurationFileSystemWatcher_Changed;
            configurationFileSystemWatcher.Created += ConfigurationFileSystemWatcher_Changed;
            configurationFileSystemWatcher.Deleted += ConfigurationFileSystemWatcher_Changed;
            configurationFileSystemWatcher.Renamed += ConfigurationFileSystemWatcher_Changed;

            configurationFileSystemWatcher.EnableRaisingEvents = true;
        }

        private void ConfigurationFileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            //ServiceUtils.WriteLog("Configuration file changed");

            //timer is used to wait 1 second before reload configurations
            configurationTimer.Stop();
            configurationTimer.Start();
        }

        private void StopMonitoringConfigurationChanges()
        {
            if (configurationFileSystemWatcher == null)
                return;

            configurationFileSystemWatcher.EnableRaisingEvents = false;
            configurationFileSystemWatcher = null;
        }

        private void ConfigurationTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            configurationTimer.Stop();
            LoadConfiguration();
            StartSync();
        }
        
        private Newtonsoft.Json.JsonSerializerSettings GetJsonSeriallizerSettings()
        {
            var jsonSeriallizerSettings = new Newtonsoft.Json.JsonSerializerSettings();
            jsonSeriallizerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            return jsonSeriallizerSettings;
        }

        private string GetConfigurationJson(string path)
        {
            string json;
            using (var streamReader = new System.IO.StreamReader(path))
            {
                json = streamReader.ReadToEnd();
            }

            return json;
        }

        #endregion

        #endregion

        #region SyncData

        private void StartSync()
        {
            foreach (var item in syncActivitiesArray)
                item.Stop();

            if (configuration == null)
                return;

            syncActivitiesArray = configuration.SyncConfigurations
                .Select(a => new SyncActivity(a, 
                    configuration.AwsConfigurations.FirstOrDefault(b => string.Equals(b.Name, a.AwsConfigurationName, StringComparison.CurrentCultureIgnoreCase))))
                        .ToArray();

            foreach (var item in syncActivitiesArray)
                item.Start();
        }

        #endregion

        #region Service Events

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Stop();
        }

        #endregion
    }
}
