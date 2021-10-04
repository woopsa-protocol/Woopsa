namespace Woopsa
{
    public class WoopsaMethodArgumentInfo : IWoopsaMethodArgumentInfo
    {
        #region Constructor

        public WoopsaMethodArgumentInfo(string name, WoopsaValueType type)
        {
            Name = name;
            Type = type;
        }

        #endregion

        #region Public Properties

        public string Name { get; }

        public WoopsaValueType Type { get; }

        #endregion
    }
}