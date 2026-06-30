using Ecs.ConfigTool;
using Ecs.LogTool;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecs.Log
{
    /// <summary>
    /// 后台定时清理日志数量任务
    /// </summary>
    public class LogCntManageJob : IHostedService, IDisposable
    {
        private readonly Timer mTimer;
        private readonly int mDelayTime;
        private readonly IOptions<ConfigOptions> _options;

        public LogCntManageJob(IOptions<ConfigOptions> options)
        {
            _options = options;
            mDelayTime = _options.Value.LogClearInterval;
            mTimer = new Timer(DoWork, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            mTimer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            mTimer.Change(5000, Timeout.Infinite); //开始任务时清理一次
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            mTimer.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        private void DoWork(object obj)
        {
            mTimer.Change(Timeout.Infinite, Timeout.Infinite);

            //Do something
            SqliteLogHelper.DeleteLogItems(_options.Value.LogMaxVolume);


            mTimer.Change(mDelayTime, Timeout.Infinite);
        }
    }
}