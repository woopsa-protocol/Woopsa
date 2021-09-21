namespace Woopsa
{
    public class WoopsaClientRequest
    {
        #region Properties

        public ClientRequest Request { get; internal set; }

        public bool IsDone { get; internal set; }

        public WoopsaClientRequestResult Result { get; internal set; }

        #endregion
    }
}
