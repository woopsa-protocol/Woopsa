namespace Woopsa
{
    public class WoopsaMetaResult
    {
        #region Properties

        public string Name { get; set; }
        public string[] Items { get; set; }
        public WoopsaPropertyMeta[] Properties { get; set; }
        public WoopsaMethodMeta[] Methods { get; set; }

        #endregion
    }

}

