using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Ecs;

public class EcsSignalHub : Hub
{
    public async Task NotifyClients(string clientFunc, object data)
    {
        try
        {
            await Clients.All.SendAsync(clientFunc, data).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}