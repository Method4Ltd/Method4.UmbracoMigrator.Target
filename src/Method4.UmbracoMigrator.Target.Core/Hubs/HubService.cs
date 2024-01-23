using Microsoft.AspNetCore.SignalR;

namespace Method4.UmbracoMigrator.Target.Core.Hubs
{
    internal class HubService
    {
        private readonly IHubContext<MigrationHub> _hubContext;

        /// <summary>
        /// Construct an new HubClientService (via DI)
        /// </summary>
        public HubService(IHubContext<MigrationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Send an 'add' message to the client 
        /// </summary>
        public void SendMessage(int phaseNum, string message)
        {
            var dateTime = DateTime.Now;
            _hubContext.Clients.All.SendAsync("MigrationStatus", phaseNum, message, dateTime).Wait();
        }
    }
}