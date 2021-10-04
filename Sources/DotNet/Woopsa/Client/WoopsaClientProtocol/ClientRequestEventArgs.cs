using System;
using System.Collections.Specialized;

namespace Woopsa
{
    public class ClientRequestEventArgs : EventArgs
    {
        #region Constructor

        public ClientRequestEventArgs(string path, NameValueCollection postData)
        {
            Path = path;
            PostData = postData;
        }

        #endregion

        #region Properties

        public string Path { get; private set; }

        public NameValueCollection PostData { get; private set; }

        #endregion
    }
}
