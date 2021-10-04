using System;

namespace Woopsa
{
    public class WoopsaLogEventArgs : EventArgs
    {
        #region Constructor

        public WoopsaLogEventArgs(WoopsaVerb verb, string path, WoopsaValue[] arguments,
            string result, bool isSuccess)
        {
            Verb = verb;
            Path = path;
            Arguments = arguments;
            Result = result;
            IsSuccess = isSuccess;
        }

        #endregion

        #region Properties

        public WoopsaVerb Verb { get; private set; }

        public string Path { get; private set; }

        public WoopsaValue[] Arguments { get; private set; }

        /// <summary>
        /// Result is valid only when IsSuccess is true. Otherwise, it contains the error message.
        /// </summary>
        public string Result { get; private set; }

        public bool IsSuccess { get; private set; }

        #endregion
    }
}
