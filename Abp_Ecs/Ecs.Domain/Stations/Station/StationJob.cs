using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Ecs.Stations;

/// <summary>
/// 各站点执行的后台任务
/// </summary>
public class StationJob : IHostedService, IDisposable
{
    private readonly StationManager _stationManager;

    public StationJob(
        StationManager stationManager)
    {
        _stationManager = stationManager;
    }

    public void Dispose()
    {

    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            await Task.Delay(3000).ConfigureAwait(false);
            var stations = await _stationManager.GetAllStationsAsync().ConfigureAwait(false);
            foreach(var station in stations)
            {
                var executor = _stationManager.CreateExecutor(station.StationCode);
                bool r = await executor.InitExecutor().ConfigureAwait(false);
                if(!r)
                    continue;
                _ = executor.Execute();
            }
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
