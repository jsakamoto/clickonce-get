using System.Collections.Generic;
using System.Threading.Tasks;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;

namespace ClickOnceGet.Server.Services
{
    public class ServerSideClickOnceAppInfoProvider : IClickOnceAppInfoProvider
    {
        private IClickOnceFileRepository ClickOnceFileRepository { get; }

        public ServerSideClickOnceAppInfoProvider(IClickOnceFileRepository clickOnceFileRepository)
        {
            ClickOnceFileRepository = clickOnceFileRepository;
        }

        public Task<IEnumerable<ClickOnceAppInfo>> GetClickOnceAppsAsync()
        {
            return Task.FromResult(this.ClickOnceFileRepository.EnumAllApps());
        }
    }
}
