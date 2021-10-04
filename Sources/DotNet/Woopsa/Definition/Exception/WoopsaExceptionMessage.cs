namespace Woopsa
{
    public static class WoopsaExceptionMessage
    {
        #region Static Methods

        public static string WoopsaCastTypeMessage(string destinationType, string sourceType)
        {
            return string.Format("Cannot typecast woopsa value of type {0} to type {1}", sourceType, destinationType);
        }

        public static string WoopsaCastValueMessage(string destinationType, string sourceValue)
        {
            return string.Format("Cannot typecast woopsa value {0} to type {1}", sourceValue, destinationType);
        }

        public static string WoopsaElementNotFoundMessage(string path)
        {
            return string.Format("Cannot find WoopsaElement specified by path {0}", path);
        }

        #endregion
    }
}
