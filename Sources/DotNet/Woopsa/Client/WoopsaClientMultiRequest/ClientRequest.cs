using System.Collections.Generic;

namespace Woopsa
{
    public class ClientRequest
    {
        #region Properties

        public int Id { get; set; }

        public string Verb { get; set; }

        public string Path { get; set; }

        public WoopsaValue Value { get; set; }

        public Dictionary<string, WoopsaValue> Arguments { get; set; }

        #endregion
    }
}
