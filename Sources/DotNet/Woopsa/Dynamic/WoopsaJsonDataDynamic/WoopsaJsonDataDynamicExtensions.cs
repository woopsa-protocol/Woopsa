namespace Woopsa
{
    public static class WoopsaJsonDataDynamicExtensions
    {
        #region Methods

        public static WoopsaJsonDataDynamic ToDynamic(this WoopsaJsonData data)
        {
            return new WoopsaJsonDataDynamic(data);
        }

        #endregion
    }
}
