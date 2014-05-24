using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace ClickOnceGet.Models
{
    public class AppDataDirRepository : IClickOnceFileRepository
    {
        private static string GetApplicationDir(string appName)
        {
            var repositoryDir = GetRepositoryDir();
            var applicationDir = Directory.GetDirectories(repositoryDir)
                .SelectMany(userDir => Directory.GetDirectories(userDir))
                .Where(appDir => Path.GetFileName(appDir).ToLower() == appName.ToLower())
                .FirstOrDefault();
            return applicationDir;
        }

        private static string GetRepositoryDir()
        {
            var repositoryDir = HttpContext.Current.Server.MapPath("~/App_Data/Repository");
            if (Directory.Exists(repositoryDir) == false) Directory.CreateDirectory(repositoryDir);
            return repositoryDir;
        }

        public byte[] GetFileContent(string appName, string subPath)
        {
            var appDir = GetApplicationDir(appName);
            if (appDir == null) return null;

            subPath = subPath.Replace('/', '\\');
            if (subPath.Split('\\').Contains("..")) return null; // reject relative up path.
            if (Path.IsPathRooted(subPath)) return null;         // reject absolute up path.

            var filePath = Path.Combine(appDir, subPath);
            if (File.Exists(filePath) == false) return null;

            return File.ReadAllBytes(filePath);
        }

        public bool GetOwnerRight(string userId, string appName)
        {
            var repositoryDir = GetRepositoryDir();
            var userDir = Path.Combine(repositoryDir, userId);
            if (Directory.Exists(userDir) == false) Directory.CreateDirectory(userDir);

            var theApp = EnumAllApps()
                .FirstOrDefault(a => a.Name.ToLower() == appName.ToLower());

            if (theApp != null) return theApp.OwnerId == userId;
            var appDir = Path.Combine(userDir, appName);
            Directory.CreateDirectory(appDir);
            return true;
        }

        public void ClearUpFiles(string appName)
        {
            var appDir = GetApplicationDir(appName);
            foreach (var dirPath in Directory.GetDirectories(appDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(dirPath, recursive: true);
            }
            foreach (var filePath in Directory.GetFiles(appDir, "*.*", SearchOption.AllDirectories))
            {
                File.Delete(filePath);
            }
        }

        public void SaveFileContent(string appName, string subPath, byte[] contents)
        {
            var appDir = GetApplicationDir(appName);
            
            subPath = subPath.Replace('/', '\\');
            if (subPath.Split('\\').Contains("..")) return;  // reject relative up path.
            if (Path.IsPathRooted(subPath)) return;         // reject absolute up path.

            var filePath = Path.Combine(appDir, subPath);
            var fileDir = Path.GetDirectoryName(filePath);
            if (Directory.Exists(fileDir) == false) Directory.CreateDirectory(fileDir);

            File.WriteAllBytes(filePath, contents);
        }

        public IEnumerable<ClickOnceAppInfo> EnumAllApps()
        {
            var repositoryDir = GetRepositoryDir();
            var apps = from userDir in Directory.GetDirectories(repositoryDir)
                       from appDir in Directory.GetDirectories(userDir)
                       let appFile = Directory.GetFiles(appDir, "*.application").FirstOrDefault()
                       where appFile != null
                       select new ClickOnceAppInfo
                       {
                           Name = Path.GetFileName(appDir),
                           OwnerId = Path.GetFileName(userDir),
                           RegisteredAt = File.GetLastWriteTimeUtc(appFile)
                       };
            return apps;
        }

        public void DeleteApp(string appName)
        {
            var appDir = GetApplicationDir(appName);
            if (appDir == null) return;

            Directory.Delete(appDir, recursive: true);
        }
    }
}