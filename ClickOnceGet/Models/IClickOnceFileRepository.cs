using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClickOnceGet.Models
{
    public interface IClickOnceFileRepository
    {
        byte[] GetDefaultFile(string appName);

        byte[] GetFile(string appName, string subPath);

        bool GetOwnerRight(string userId, string appName);

        void ClearAllFiles(string appName);

        void SetFile(string appName, string subPath, byte[] contents);

        IEnumerable<ClickOnceAppInfo> EnumAllApplications();

        void DeleteApp(string appName);
    }
}
