using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enginesoft.SyncS3.Service
{
    public static class ServiceUtils
    {
        private static object lockWrite = new object();
        private static string serviceFolder = null;

        public static void WriteLog(string message)
        {
            message = string.Format("{0}\t\t{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
            System.Diagnostics.Debug.WriteLine(message);

            string path = GetLogPath();

            lock (lockWrite)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(message);
                }
            }
        }

        public static void ClearLog()
        {
            string path = GetLogPath();

            lock (lockWrite)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    writer.Write(string.Empty);
                }
            }
        }
        
        public static string GetRelativePath(string path, string basePath)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentNullException(nameof(basePath));

            if (!path.StartsWith(basePath))
                throw new System.InvalidOperationException(string.Format("path: \"{0}\" is invalid for basePath: \"{1}\"", path, basePath));

            string relativePath = path.Substring(basePath.Length);

            if (relativePath.StartsWith("\\"))
                relativePath = relativePath.Substring(1);

            relativePath = relativePath.Replace("\\", "/");

            return relativePath;
        }

        public static string GetLogPath()
        {
            string path = System.IO.Path.Combine(GetLogFolder(), string.Format("log-{0}.txt", DateTime.Now.ToString("yyyy-MM-dd")));
            return path;
        }

        public static string GetConfigurationPath()
        {
            string path = System.IO.Path.Combine(GetConfigurationFolder(), "main.json");
            return path;
        }

        public static string GetConfigurationFolder()
        {
            return System.IO.Path.Combine(GetServiceFolder(), "Config");
        }

        public static string GetLogFolder()
        {
            string path = System.IO.Path.Combine(GetServiceFolder(), "Log");
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
            return path;
        }

        public static string GetServiceFolder()
        {
            if (serviceFolder != null)
                return serviceFolder;

            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            serviceFolder = System.IO.Path.GetDirectoryName(path);
            return serviceFolder;
        }
    }
}
