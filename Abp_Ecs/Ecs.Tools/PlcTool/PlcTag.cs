namespace Ecs.PlcTool
{
    public class PlcTag
    {
        private string mTagName;
        private string mTagAddr;
        private EnumPlcTagType mTagType;
        private EnumTagAccess mTagAccess;
        private bool mIsPublish;

        public string TagName => mTagName;

        public string TagAddr => mTagAddr;

        public EnumPlcTagType TagType => mTagType;

        public EnumTagAccess TagAccess => mTagAccess;

        public bool IsPublish => mIsPublish;

        public PlcTag(string tagName, string tagAddr, EnumPlcTagType tagType, EnumTagAccess tagAccess, bool isPublish)
        {
            mTagName = tagName;
            mTagAddr = tagAddr;
            mTagType = tagType;
            mTagAccess = tagAccess;
            mIsPublish = isPublish;
        }
    }
}
