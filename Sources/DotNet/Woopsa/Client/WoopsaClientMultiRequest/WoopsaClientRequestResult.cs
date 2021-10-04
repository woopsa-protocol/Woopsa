using System;

namespace Woopsa
{
    public class WoopsaClientRequestResult
    {
        #region Properties

        public WoopsaClientRequestResultType ResultType { get; internal set; }

        public Exception Error { get; internal set; }

        public WoopsaValue Value { get; internal set; }

        public WoopsaMetaResult Meta { get; internal set; }

        #endregion
    }

}
