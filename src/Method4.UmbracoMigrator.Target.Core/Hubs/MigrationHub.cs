using Microsoft.AspNetCore.SignalR;

namespace Method4.UmbracoMigrator.Target.Core.Hubs
{
    public class MigrationHub : Hub
    {
        public async Task SendMessage(int phaseNum, string message) // Not used
        {
            await Clients.All.SendAsync("MigrationStatus", phaseNum, message);
        }
    }
}