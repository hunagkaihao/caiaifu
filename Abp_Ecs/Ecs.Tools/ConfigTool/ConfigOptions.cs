using System.Collections.Generic;

namespace Ecs.ConfigTool
{
    public class PlcHeartBeatSet
    {
        public string PlcName { get; set; } = string.Empty;
        public string HeartTagName { get; set; } = string.Empty; //需要为整型数据
        public int CycleTime { get; set; }
    }

    public class ConfigOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        
        public string HubCliMethod_UpdatePlcTags { get; set; } = string.Empty;

        public string SqliteLogConnString { get; set; } = "";
        public int LogClearInterval { get; set; } = 0;
        public int LogMaxVolume { get; set; } = 0;
        public string RedisConnStr { get; set; } = "";
        public int DefaultRedisNo { get; set; } = 0;
        public int PlcRedisNo { get; set; } = 0;

        public bool RemovePlcTagTempValueOnStart { get; set; }
        public List<PlcHeartBeatSet> HeartBeatsFromPlc { get; set; } = new List<PlcHeartBeatSet>();
        public List<PlcHeartBeatSet> HeartBeatsToPlc { get; set; } = new List<PlcHeartBeatSet>();
        public List<string> PlcTagMonitors { get; set; } = new List<string>();

    }
}
