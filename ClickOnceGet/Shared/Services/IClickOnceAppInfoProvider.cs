using System.Collections.Generic;
using System.Threading.Tasks;
using ClickOnceGet.Shared.Models;

namespace ClickOnceGet.Shared.Services
{
    public interface IClickOnceAppInfoProvider
    {
        Task<IEnumerable<ClickOnceAppInfo>> GetAllAppsAsync();

        Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync();
    }
}
