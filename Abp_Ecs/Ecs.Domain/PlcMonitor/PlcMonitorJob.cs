using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecs.ConfigTool;
using Ecs.RedisTool;
using Ecs.LogTool;
using Ecs.PlcTool;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ecs.HttpApiTool;
using Ecs.SignalRTool;

namespace Ecs.PlcMonitor;

/// <summary>
/// 后台PLC变量监控以及心跳监控
/// </summary>
public class PlcMonitorJob : IHostedService, IDisposable
{
    private readonly PlcHelper _plcHelper;
    private readonly ILogger<PlcMonitorJob> _logger;
    private readonly IRedisClient _ecsRedisClient;
    private readonly IOptions<ConfigOptions> _options;
    private readonly PlcMonitorManager _monitorManager;
    private readonly HubMsgQHelper _hubHelper;

    
    private Timer[] mTimersForHeartBeatFromPlc;
    private Timer[] mTimersForHeartBeatToPlc;
    private string[] mLastHeartBeatValFromPlc;
    private string[] mLastHeartBeatValToPlc;

    public PlcMonitorJob(
        PlcHelper plcHelper, 
        ILogger<PlcMonitorJob> logger, 
        IRedisClient redisClient,
        IOptions<ConfigOptions> options,
        PlcMonitorManager plcMonitorManager,
        HubMsgQHelper hubHelper)
    {
        try
        {
            _plcHelper = plcHelper;
            _logger = logger;
            _options = options;
            _monitorManager = plcMonitorManager;
            _hubHelper = hubHelper;
            _ecsRedisClient = redisClient; //记录monitor结果
            _ecsRedisClient.Build(options.Value.RedisConnStr, options.Value.DefaultRedisNo);

            int heartBeatNumFromPlc = options.Value.HeartBeatsFromPlc.Count;
            if (heartBeatNumFromPlc <= 0)
            {
                mTimersForHeartBeatFromPlc = new Timer[0];
                mLastHeartBeatValFromPlc = new string[0];
            }
            else
            {
                mTimersForHeartBeatFromPlc = new Timer[heartBeatNumFromPlc];
                mLastHeartBeatValFromPlc = new string[heartBeatNumFromPlc];
                for (int i = 0; i < heartBeatNumFromPlc; i++)
                {
                    Timer timer = new Timer(HeartBeatFromPlc, i, Timeout.Infinite, Timeout.Infinite);
                    mTimersForHeartBeatFromPlc[i] = timer;
                    mLastHeartBeatValFromPlc[i] = "";
                }
            }

            int heartBeatNumToPlc = options.Value.HeartBeatsToPlc.Count;
            if (heartBeatNumToPlc <= 0)
            {
                mTimersForHeartBeatToPlc = new Timer[0];
                mLastHeartBeatValToPlc = new string[0];
            }
            else
            {
                mTimersForHeartBeatToPlc = new Timer[heartBeatNumToPlc];
                mLastHeartBeatValToPlc = new string[heartBeatNumToPlc];
                for (int i = 0; i < heartBeatNumToPlc; i++)
                {
                    Timer timer = new Timer(HeartBeatToPlc, i, Timeout.Infinite, Timeout.Infinite);
                    mTimersForHeartBeatToPlc[i] = timer;
                    mLastHeartBeatValToPlc[i] = "";
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void Dispose()
    {
        foreach(var timer in mTimersForHeartBeatFromPlc)
        {
            timer.Dispose();
        }
        foreach(var timer in mTimersForHeartBeatToPlc)
        {
            timer.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            await Task.Delay(3000).ConfigureAwait(false);
            StartPlcTagMonitor();
            StartHeartBeatMonitor();
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private bool StartPlcTagMonitor()
    {
        try
        {
            string[] _fields = _ecsRedisClient.GetHashFields(EcsConsts.MonitorChannelName);
            _ecsRedisClient.RemoveHashFields(EcsConsts.MonitorChannelName, _fields); //删除监控通道的所有字段

            List<string> monitorTags = _options.Value.PlcTagMonitors;
            if(monitorTags.Count == 0)
                return false;

            foreach (string tag in monitorTags)
            {
                string[] sects = tag.Split(".", StringSplitOptions.RemoveEmptyEntries);
                if(sects.Length != 2)
                    continue;

                string _plcName = sects[0];
                string _tagName = sects[1];

                if (string.IsNullOrEmpty(_plcName) ||
                    string.IsNullOrEmpty(_tagName))
                    continue;

                if (true != _plcHelper.IsPlcTagExist(_plcName, _tagName))
                    continue;

                PlcTagValue _val = _plcHelper.ReadPlcTag(_plcName, _tagName);

                MonitorValue value = new MonitorValue
                {
                    monitorTagName = $"{_plcName}.{_tagName}"
                };

                if (_val != null)
                {
                    value.monitorTagAddr = _val.Tag.TagAddr;
                    value.monitorTagQuality = _val.Quality.ToString();
                    value.timeStamp = _val.TimeStamp;
                    
                    if(_val.Tag.TagType == EnumPlcTagType.U8Array)
                    {
                        byte[] baVal = System.Text.Encoding.GetEncoding(28591).GetBytes(_val.Value);
                        string tmp = string.Empty;
                        foreach(byte b in baVal)
                        {
                            tmp = $"{tmp},{b}";
                        }
                        value.monitorTagValue = tmp == string.Empty ? tmp : tmp.Substring(1);
                    }
                    else
                        value.monitorTagValue = _val.Value;
                }

                string jsonVal = JsonConvert.SerializeObject(value);
                UpdateMonitorValue($"{_plcName}.{_tagName}", jsonVal);

                _plcHelper.Subscribe(_plcName, _tagName, (_plcName_, _tagName_, _tagValue_)=>{
                    try
                    {
                        if (_tagValue_ == null)
                            return;

                        MonitorValue _value = new MonitorValue
                        {
                            monitorTagName = $"{_plcName_}.{_tagName_}",
                            monitorTagAddr = _tagValue_.Tag.TagAddr,
                            monitorTagQuality = _tagValue_.Quality.ToString(),
                            timeStamp = _tagValue_.TimeStamp
                        };

                        if (_tagValue_.Tag.TagType == EnumPlcTagType.U8Array)
                        {
                            byte[] baVal = System.Text.Encoding.GetEncoding(28591).GetBytes(_tagValue_.Value);
                            string tmp = string.Empty;
                            foreach(byte b in baVal)
                            {
                                tmp = $"{tmp},{b}";
                            }
                            _value.monitorTagValue = tmp == string.Empty ? tmp : tmp.Substring(1);
                        }
                        else
                            _value.monitorTagValue = _tagValue_.Value;

                        string _jsonVal = JsonConvert.SerializeObject(_value);
                        UpdateMonitorValue($"{_plcName_}.{_tagName_}", _jsonVal);
                        
                        List<MonitorValue> values = _monitorManager.GetAllMonitorValuesAsync().GetAwaiter().GetResult();
                        _hubHelper.SendMessage(_options.Value.HubCliMethod_UpdatePlcTags, values);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.Message);
                    }
                });
            }

            return true;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    private void StartHeartBeatMonitor()
    {
        int _count = mTimersForHeartBeatFromPlc.Length;
        if (_count > 0)
        {
            for(int i = 0; i < _count; i++)
                mTimersForHeartBeatFromPlc[i].Change(_options.Value.HeartBeatsFromPlc[i].CycleTime, Timeout.Infinite);
        }
        
        _count = mTimersForHeartBeatToPlc.Length;
        if (_count > 0)
        {
            for(int i = 0; i < _count; i++)
                mTimersForHeartBeatToPlc[i].Change(_options.Value.HeartBeatsToPlc[i].CycleTime, Timeout.Infinite);
        }
    }

    private void HeartBeatFromPlc(object state)
    {
        try
        {
            int _idx = (int)state; //state为计时器的索引
            List<PlcHeartBeatSet> _sets = _options.Value.HeartBeatsFromPlc;
            if (_idx >= _sets.Count || _idx < 0)
                return;

            mTimersForHeartBeatFromPlc[_idx].Change(Timeout.Infinite, Timeout.Infinite); //停止计时

            PlcHeartBeatSet _plcHeartBeatSet = _sets[_idx];
            string _plcName = _plcHeartBeatSet.PlcName;
            string _heartTag = _plcHeartBeatSet.HeartTagName;
            int _cycleTime = _plcHeartBeatSet.CycleTime;

            PlcTagValue _valueThisTime = _plcHelper.ReadPlcTag(_plcName, _heartTag);
            if (_valueThisTime == null || _valueThisTime.Quality == EnumQuality.Bad) //读不到心跳信号的值，认为PLC没有心跳
            {
                UpdateMonitorValue($"{_plcName}.{_heartTag}", false.ToString());
                mTimersForHeartBeatFromPlc[_idx].Change(_cycleTime, Timeout.Infinite);
                return;
            }

            if (_valueThisTime.Value != mLastHeartBeatValFromPlc[_idx])
            {
                mLastHeartBeatValFromPlc[_idx] = _valueThisTime.Value;
                UpdateMonitorValue($"{_plcName}.{_heartTag}", true.ToString());
                mTimersForHeartBeatFromPlc[_idx].Change(_cycleTime, Timeout.Infinite);
                return;
            }

            UpdateMonitorValue($"{_plcName}.{_heartTag}", false.ToString());

            mTimersForHeartBeatFromPlc[_idx].Change(_cycleTime, Timeout.Infinite);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    private void HeartBeatToPlc(object state)
    {
        try
        {
            int _idx = (int)state!; //state为计时器的索引
            List<PlcHeartBeatSet> _sets = _options.Value.HeartBeatsToPlc;
            if (_idx >= _sets.Count || _idx < 0)
                return;

            mTimersForHeartBeatToPlc[_idx].Change(Timeout.Infinite, Timeout.Infinite); //停止计时

            PlcHeartBeatSet _plcHeartBeatSet = _sets[_idx];
            string _plcName = _plcHeartBeatSet.PlcName;
            string _heartTag = _plcHeartBeatSet.HeartTagName;
            int _cycleTime = _plcHeartBeatSet.CycleTime;

            string _lastVal = mLastHeartBeatValToPlc[_idx];
            if(_lastVal == "" || _lastVal.ToLower() == "false")
            {   
                _plcHelper.WritePlcTag(_plcName, _heartTag, true.ToString());
                mLastHeartBeatValToPlc[_idx] = "true";
            }
            else
            {    
                _plcHelper.WritePlcTag(_plcName, _heartTag, false.ToString());
                mLastHeartBeatValToPlc[_idx] = "false";
            }

            mTimersForHeartBeatToPlc[_idx].Change(_cycleTime, Timeout.Infinite);
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public bool UpdateMonitorValue(string monitorName, string monitorVal)
    {
        try
        {
            _ecsRedisClient.SetHashValue(EcsConsts.MonitorChannelName, monitorName, monitorVal);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateMonitorValueAsync(string monitorName, string monitorVal)
    {
        try
        {
            await _ecsRedisClient.SetHashValueAsync(EcsConsts.MonitorChannelName, monitorName, monitorVal);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }
}
