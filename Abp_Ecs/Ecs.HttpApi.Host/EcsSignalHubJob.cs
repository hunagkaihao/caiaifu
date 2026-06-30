using System;
using System.Threading;
using System.Threading.Tasks;
using Ecs.LogTool;
using Ecs.SignalRTool;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace Ecs
{
    /// <summary>
    /// 向hub客户端发送消息
    /// </summary>
    public class EcsSignalHubJob : IHostedService, IDisposable
    {
        private readonly IHubContext<EcsSignalHub> _hubContext;
        private readonly HubMsgQHelper _hubMsgQHelper;
        private readonly ILogger<EcsSignalHubJob> _logger;

        public EcsSignalHubJob(
            IHubContext<EcsSignalHub> hubContext,
            HubMsgQHelper hubMsgQueueHelper,
            ILogger<EcsSignalHubJob> logger)
        {
            _hubContext = hubContext;
            _hubMsgQHelper = hubMsgQueueHelper;
            _logger = logger;
        }

        public void Dispose()
        {
            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                await Task.Delay(3000).ConfigureAwait(false);
                await DoWork().ConfigureAwait(false);
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            while(true)
            {
                try
                {
                    await Task.Delay(100);
                    HubMessage msg = _hubMsgQHelper.GetMessage();
                    if(msg == null)
                        continue;

                    await _hubContext.Clients.All.SendAsync(msg.CliMethod, msg.Data).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    _logger.Error(ex.Message);
                }
            }
        }
    }
}