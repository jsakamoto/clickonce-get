﻿using ClickOnceGet.Shared.Models;

namespace ClickOnceGet.Server.Services;

public interface IClickOnceFileRepository
{
    ClickOnceAppInfo GetAppInfo(string appName);

    byte[] GetFileContent(string appName, string subPath);

    bool GetOwnerRight(string userId, string appName);

    void ClearUpFiles(string appName);

    void SaveAppInfo(string appName, ClickOnceAppInfo appInfo);

    void SaveFileContent(string appName, string subPath, byte[] contents);

    IEnumerable<ClickOnceAppInfo> EnumAllApps();

    void DeleteApp(string appName);
}
