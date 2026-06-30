using System;

namespace Ecs.PlcTool
{
    public class PlcTagValue
    {
        private PlcTag mTag;
        private string mValue;
        private EnumQuality mQuality;
        private string mTimeStamp;

        public PlcTag Tag => mTag;

        public string Value
        {
            get => mValue;
            set => mValue = value;
        }

        public EnumQuality Quality
        {
            get => mQuality;
            set => mQuality = value;
        }

        public string TimeStamp
        {
            get => mTimeStamp;
            set => mTimeStamp = value;
        }

        public PlcTagValue(PlcTag tag)
        {
            mTag = tag;
            mValue = "";
            mQuality = EnumQuality.Bad;
            mTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
