using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Hosting;

namespace ClickOnceGet.Server.Services
{
    public class AppDataDirRepository : IClickOnceFileRepository
    {
        private IWebHostEnvironment WebHostEnv { get; }

        public AppDataDirRepository(IWebHostEnvironment webHostEnv)
        {
            WebHostEnv = webHostEnv;
        }

        private string GetRepositoryDir()
        {
            var repositoryDir = Path.Combine(WebHostEnv.ContentRootPath, "App_Data", "Repository");
            if (Directory.Exists(repositoryDir) == false) Directory.CreateDirectory(repositoryDir);
            return repositoryDir;
        }

        private string GetApplicationDir(string appName)
        {
            var repositoryDir = GetRepositoryDir();
            var applicationDir = Directory.GetDirectories(repositoryDir)
                .SelectMany(userDir => Directory.GetDirectories(userDir))
                .Where(appDir => Path.GetFileName(appDir).ToLower() == appName.ToLower())
                .FirstOrDefault();
            return applicationDir;
        }

        private string GetAppInfoPath(string appName)
        {
            var appDir = GetApplicationDir(appName);
            if (appDir == null) return null;
            var appInfoPath = Path.Combine(GetApplicationDir(appName), ".appinfo");
            return appInfoPath;
        }

        public ClickOnceAppInfo GetAppInfo(string appName)
        {
            return EnumAllApps().FirstOrDefault(app => app.Name.ToLower() == appName.ToLower());
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

        public void SaveAppInfo(string appName, ClickOnceAppInfo appInfo)
        {
            var appInfoPath = GetAppInfoPath(appName);
            File.WriteAllText(appInfoPath, JsonSerializer.Serialize(appInfo));
        }

        public IEnumerable<ClickOnceAppInfo> EnumAllApps()
        {
            var repositoryDir = GetRepositoryDir();
            var apps = from userDir in Directory.GetDirectories(repositoryDir)
                       let ownerId = Path.GetFileName(userDir)
                       from appDir in Directory.GetDirectories(userDir)
                       let appFilePath = Directory.GetFiles(appDir, "*.application").FirstOrDefault()
                       where appFilePath != null
                       let appInfoPath = Directory.GetFiles(appDir, ".appinfo").FirstOrDefault()
                       select new { appDir, ownerId, appFilePath, appInfoPath };

            return apps.Select(app =>
            {
                var appInfo = app.appInfoPath switch
                {
                    null => new ClickOnceAppInfo(),
                    _ => JsonSerializer.Deserialize<ClickOnceAppInfo>(File.ReadAllText(app.appInfoPath))
                };
                appInfo.Name = Path.GetFileName(app.appDir);
                appInfo.OwnerId = app.ownerId;
                appInfo.RegisteredAt = File.GetLastWriteTimeUtc(app.appFilePath);
                return appInfo;
            });
        }

        public void DeleteApp(string appName)
        {
            var appDir = GetApplicationDir(appName);
            if (appDir == null) return;

            Directory.Delete(appDir, recursive: true);
        }
    }
}