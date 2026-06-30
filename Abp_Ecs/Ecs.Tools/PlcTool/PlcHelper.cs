using System;
using Ecs.ConfigTool;
using Ecs.RedisTool;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Ecs.PlcTool;
    public delegate void PlcTagValueChanged(string plcName, string tagName, PlcTagValue tagNewValue);

    public class PlcHelper : ISingletonDependency
    {
        private readonly IRedisClient _ecsRedisClient;
        private readonly IRedisClient _plcRedisClient;
        private readonly ILogger<PlcHelper> _logger;
        private readonly IOptions<ConfigOptions> _options;

        private const string PlcServerRegChannelName = "RegisterChannel"; //向Plc服务注册的通道名称
        private const string TagTempChannelName = "Plc.TriggerTemp"; //Plc触发变量缓存通道名称

        private bool mPlcServerRegistered;
        private string mPlcRedisClientName;

        public PlcHelper(
            IOptions<ConfigOptions> options, 
            ILogger<PlcHelper> logger, 
            IRedisClient plcRedisClient, 
            IRedisClient EcsRedisClient)
        {
            try
            {
                _options = options;
                _logger = logger;
                _plcRedisClient = plcRedisClient;
                _ecsRedisClient = EcsRedisClient;
                _plcRedisClient.Build(_options.Value.RedisConnStr, _options.Value.PlcRedisNo);
                _ecsRedisClient.Build(_options.Value.RedisConnStr, _options.Value.DefaultRedisNo);
                
                mPlcServerRegistered = false;
                mPlcRedisClientName = Dns.GetHostName();

                RegisterPlcServerClient(); //mPlcRedisClient注册到Plc服务器
                if(_options.Value.RemovePlcTagTempValueOnStart)
                {
                    string[] fields = _ecsRedisClient.GetHashFields(TagTempChannelName);
                    _ecsRedisClient.RemoveHashFields(TagTempChannelName, fields);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// 判断Plc变量是否存在
        /// </summary>
        /// <param name="plcName"></param>
        /// <param name="tagName"></param>
        /// <returns>true：存在，false：不存在，null：发生错误</returns>
        public bool? IsPlcTagExist(string plcName, string tagName)
        {
            try
            {
                return _plcRedisClient.IsKeyExist($"{plcName}.{tagName}");
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 读取Plc变量值
        /// </summary>
        /// <param name="plcName"></param>
        /// <param name="tagName"></param>
        /// <returns>发生错误返回null</returns>
        public PlcTagValue ReadPlcTag(string plcName, string tagName)
        {
            try
            {
                string val = _plcRedisClient.GetStringValue($"{plcName}.{tagName}");
                if (val == null)
                {
                    _logger.Error($"PLC变量 {plcName}.{tagName} 不存在，无法读取");
                    return null;
                }

                return JsonConvert.DeserializeObject<PlcTagValue>(val);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 异步读取Plc变量值
        /// </summary>
        /// <param name="plcName"></param>
        /// <param name="tagName"></param>
        /// <returns>发生错误返回null</returns>
        public async Task<PlcTagValue> ReadPlcTagAsync(string plcName, string tagName)
        {
            try
            {
                string val = await _plcRedisClient.GetStringValueAsync($"{plcName}.{tagName}");
                if (val == null)
                {
                    _logger.Error($"PLC变量 {plcName}.{tagName} 不存在，无法读取");
                    return null;
                }

                return JsonConvert.DeserializeObject<PlcTagValue>(val);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 写Plc变量
        /// </summary>
        /// <param name="plcName"></param>
        /// <param name="tagName"></param>
        /// <param name="tagValue"></param>
        /// <returns></returns>
        public bool WritePlcTag(string plcName, string tagName, string tagValue)
        {         
            string tempChannel = Guid.NewGuid().ToString();//接收写PLC操作结果的临时频道
            try
            {
                if (!mPlcServerRegistered)
                {
                    RegisterPlcServerClient();
                    Thread.Sleep(500);
                }
                if (!mPlcServerRegistered)
                {
                    _logger.Error($"该客户端在Plc服务器上注册失败,无法将值{tagValue}写入变量{plcName}.{tagName}");
                    return false;
                }

                bool ret = _plcRedisClient.Publish(mPlcRedisClientName, $"{plcName}@#${tagName}@#${tagValue}@#${tempChannel}");
                
                if (!ret) //Plc服务器没有收到发布
                {
                    RegisterPlcServerClient(); //可能Plc服务被关闭了，尝试重新注册
                    if (!mPlcServerRegistered) //没有注册成功
                    {
                        string msg = $"Plc服务可能被关闭，客户端{mPlcRedisClientName}在Plc服务器上注册失败,无法写Tag：{plcName}.{tagName}";
                        _logger.Error(msg);
                        return false;
                    }
                    Thread.Sleep(500);
                    //成功注册，重新尝试写变量
                    ret = _plcRedisClient.Publish(mPlcRedisClientName, $"{plcName}@#${tagName}@#${tagValue}@#${tempChannel}");
                }
                if (!ret)
                {
                    string msg = $"Plc服务可能被关闭，客户端{mPlcRedisClientName}发布的写变量{plcName}.{tagName}命令没有被PLC服务器接收到";
                    _logger.Error(msg);
                    return false;
                }

                ret = false;
                long point1 = DateTime.Now.Ticks;
                while(true)
                {
                    Thread.Sleep(10);
                    long point2 = DateTime.Now.Ticks;
                    TimeSpan ts = new TimeSpan(point2 - point1);
                    if (ts.TotalMilliseconds > 5000) //超过5s，视为写操作失败
                    {
                        string msg = $"客户端{mPlcRedisClientName}发布的写变量{plcName}.{tagName}命令，PLC服务器已接收到，但客户端没有接收到服务器的写Tag结果，已超时";
                        _logger.Error(msg);
                        break;
                    }

                    string val = _plcRedisClient.GetStringValue(tempChannel);
                    if (val == null) //还未收到写操作的反馈
                        continue;

                    if (!bool.TryParse(val, out ret)) //收到反馈
                    {
                        string msg = $"客户端{mPlcRedisClientName}发布将值{tagValue}写变量{plcName}.{tagName}的命令，PLC服务器反馈写Tag失败";
                        _logger.Error(msg);
                        ret = false;
                    }
                    _plcRedisClient.RemoveKey(tempChannel);
                    break;
                }
                return ret;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 异步写Plc变量
        /// </summary>
        /// <param name="plcName"></param>
        /// <param name="tagName"></param>
        /// <param name="tagValue"></param>
        /// <returns></returns>
        public async Task<bool> WritePlcTagAsync(string plcName, string tagName, string tagValue)
        {
            string tempChannel = Guid.NewGuid().ToString();//接收写PLC操作结果的临时频道
            try
            {
                if (!mPlcServerRegistered)
                {
                    RegisterPlcServerClient();
                    await Task.Delay(500).ConfigureAwait(false);
                }
                if (!mPlcServerRegistered)
                {
                    _logger.Error($"该客户端在Plc服务器上注册失败,无法将值{tagValue}写入变量{plcName}.{tagName}");
                    return false;
                }

                bool ret = await _plcRedisClient.PublishAsync(mPlcRedisClientName, $"{plcName}@#${tagName}@#${tagValue}@#${tempChannel}");

                if (!ret) //Plc服务器没有收到发布
                {
                    RegisterPlcServerClient(); //可能Plc服务被关闭了，尝试重新注册
                    if (!mPlcServerRegistered) //没有注册成功
                    {
                        string msg = $"Plc服务可能被关闭，客户端{mPlcRedisClientName}在Plc服务器上注册失败,无法写Tag：{plcName}.{tagName}";
                        _logger.Error(msg);
                        return false;
                    }
                    await Task.Delay(500).ConfigureAwait(false);
                    //成功注册，重新尝试写变量
                    ret = await _plcRedisClient.PublishAsync(mPlcRedisClientName, $"{plcName}@#${tagName}@#${tagValue}@#${tempChannel}");
                }
                if (!ret)
                {
                    string msg = $"Plc服务可能被关闭，客户端{mPlcRedisClientName}发布的写变量{plcName}.{tagName}命令没有被PLC服务器接收到";
                    _logger.Error(msg);
                    return false;
                }

                ret = false;
                long point1 = DateTime.Now.Ticks;
                while (true)
                {
                    await Task.Delay(10);
                    long point2 = DateTime.Now.Ticks;
                    TimeSpan ts = new TimeSpan(point2 - point1);
                    if (ts.TotalMilliseconds > 5000) //超过5s，视为写操作失败
                    {
                        string msg = $"客户端{mPlcRedisClientName}发布的写变量{plcName}.{tagName}命令，PLC服务器已接收到，但客户端没有接收到服务器的写Tag结果，已超时";
                        _logger.Error(msg);
                        break;
                    }

                    string val = await _plcRedisClient.GetStringValueAsync(tempChannel);
                    if (val == null) //还未收到写操作的反馈
                        continue;

                    if (!bool.TryParse(val, out ret)) //收到反馈
                    {
                        string msg = $"客户端{mPlcRedisClientName}发布将值{tagValue}写变量{plcName}.{tagName}的命令，PLC服务器反馈写Tag失败";
                        _logger.Error(msg);
                        ret = false;
                    }
                    await _plcRedisClient.RemoveKeyAsync(tempChannel);
                    break;
                }
                return ret;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 判断Plc变量是否发生变化
        /// </summary>
        /// <param name="plcName"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public bool IsPlcTagValueChange(string plcName, string tagName)
        {
            PlcTagValue value = ReadPlcTag(plcName, tagName);
            if (value == null || value.Quality == EnumQuality.Bad) //PLC变量不存在，或没有读到PLC值
                return false;

            string oldValue = GetPlcTagTemp(plcName, tagName);
            if (oldValue == null) //首次比较，还没有缓存值，将当前值写入缓存值，返回未变化
            {
                SetPlcTagTemp(plcName, tagName, value.Value);
                return false;
            }

            if (oldValue == "ERROR") //读取时，发生错误
            {
                return false;
            }

            if(value.Value == "0") //0表示复位值，返回未变化
            {
                SetPlcTagTemp(plcName, tagName, value.Value);
                return false;
            }

            if (oldValue != value.Value)
            {
                SetPlcTagTemp(plcName, tagName, value.Value);
                return true;
            }

            return false;
        }

        public bool Subscribe(string plcName, string tagName, PlcTagValueChanged handle)
        {
            if (true != IsPlcTagExist(plcName, tagName))
                return false;

            try
            {
                _plcRedisClient.Subscribe($"{plcName}.{tagName}", (channel, value) => {
                    PlcTagValue v = null;
                    try
                    {
                        v = JsonConvert.DeserializeObject<PlcTagValue>(value);
                    }
                    catch(Exception e)
                    {
                        _logger.Error(e.Message);
                        return;
                    }
                    handle.Invoke(plcName, tagName, v);
                });
                return true;
            }
            catch(Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }
        }

        public async Task<bool> SubscribeAsync(string plcName, string tagName, PlcTagValueChanged handle)
        {
            if (true != IsPlcTagExist(plcName, tagName))
                return false;

            try
            {
                await _plcRedisClient.SubscribeAsync($"{plcName}.{tagName}", (channel, value) => {
                    PlcTagValue v = null;
                    try
                    {
                        v = JsonConvert.DeserializeObject<PlcTagValue>(value);
                    }
                    catch(Exception e)
                    {
                        _logger.Error(e.Message);
                        return;
                    }
                    handle.Invoke(plcName, tagName, v);
                }).ConfigureAwait(false);
                return true;
            }
            catch(Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }
        }

        //private
        private bool RegisterPlcServerClient()
        {
            try
            {
                mPlcServerRegistered = _plcRedisClient.Publish(PlcServerRegChannelName, mPlcRedisClientName);
                return mPlcServerRegistered;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                mPlcServerRegistered = false;
                return false;
            }
        }

        private string GetPlcTagTemp(string plcName, string tagName)
        {
            try
            {
                return _ecsRedisClient.GetHashValue(TagTempChannelName, $"{plcName}.{tagName}");
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return "ERROR";
            }
        }

        private void SetPlcTagTemp(string plcName, string tagName, string value)
        {
            try
            {
                _ecsRedisClient.SetHashValue(TagTempChannelName, $"{plcName}.{tagName}", value);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }
        }


    }
