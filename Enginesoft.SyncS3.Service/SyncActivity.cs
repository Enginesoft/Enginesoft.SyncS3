using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enginesoft.SyncS3.Service
{
    public class SyncActivity
    {
        private System.IO.FileSystemWatcher fileSystemWatcher;
        private System.Timers.Timer timer = null;
        private Dictionary<string, string> filesQueue = new Dictionary<string, string>();
        private AwsS3Client awsS3Client = null;
        private Models.SyncConfiguration syncConfiguration = null;
        private string basePath = null;
        
        public SyncActivity(Models.SyncConfiguration syncConfiguration, Models.AwsConfiguration awsConfiguration)
        {
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Enabled = false;
            timer.Elapsed += Timer_Elapsed;

            this.syncConfiguration = new Models.SyncConfiguration(syncConfiguration.Name, 
                syncConfiguration.AwsConfigurationName, syncConfiguration.LocalPath, syncConfiguration.ServerPath, syncConfiguration.FileFilter);

            awsS3Client = new AwsS3Client(awsConfiguration.AwsCredential);

            basePath = syncConfiguration.LocalPath;
            
            fileSystemWatcher = new System.IO.FileSystemWatcher();
            fileSystemWatcher.Path = basePath;
            fileSystemWatcher.IncludeSubdirectories = true;
            if (!string.IsNullOrEmpty(syncConfiguration.FileFilter))
                fileSystemWatcher.Filter = syncConfiguration.FileFilter;
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Changed;
            fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
        }

        public void Start()
        {
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                try
                {
                    var attr = File.GetAttributes(e.FullPath);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        return; //Ignore directory change
                }
                catch (System.Exception ex)
                {
                    ServiceUtils.WriteLog(string.Format("ERROR getting file attributes -- {0}", ex.Message));
                    return;
                }
            }

            EnqueueItem(e.FullPath);

            //timer is used to wait 1 second before execute sync
            timer.Stop();
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DequeueItemAndProcess();
        }

        private void EnqueueItem(string path)
        {
            if (!filesQueue.ContainsKey(path))
                filesQueue.Add(path, path);
        }

        private void DequeueItemAndProcess()
        {
            var keys = filesQueue.Keys.ToList();
            foreach (var key in keys)
            {
                filesQueue.Remove(key);
                ProcessItem(key);
            }
        }

        private void ProcessItem(string path)
        {
            string relativePath = ServiceUtils.GetRelativePath(path, basePath);
            bool update = System.IO.File.Exists(path);
            int attempt = 0;
            int maxAttempts = 3;

            while (true)
            {
                try
                {
                    attempt++;

                    if (update)
                        UploadFile(path, relativePath);
                    else
                        DeleteFile(path, relativePath);

                    break;
                }
                catch (System.Exception ex)
                {
                    ServiceUtils.WriteLog(string.Format("ERROR {0} file: \"{1}\" -- attempt:{2}/{3} -- {4}", (update ? "updating" : "deleting"), path, attempt, maxAttempts, ex.Message));
                    System.Diagnostics.Debugger.Break();

                    if (attempt >= maxAttempts)
                        break;
                }
            }
        }
        
        private void UploadFile(string path, string relativePath)
        {
            awsS3Client.UploadFile(syncConfiguration.ServerPath, path, relativePath);
            ServiceUtils.WriteLog(string.Format("File: {0} updated in bucket: {1}, path: {2}", path, syncConfiguration.ServerPath, relativePath));
        }

        private void DeleteFile(string path, string relativePath)
        {
            awsS3Client.DeleteItem(syncConfiguration.ServerPath, relativePath);
            ServiceUtils.WriteLog(string.Format("File: {0} deleted from bucket: {1}, path: {2}", path, syncConfiguration.ServerPath, relativePath));
        }
    }
}
