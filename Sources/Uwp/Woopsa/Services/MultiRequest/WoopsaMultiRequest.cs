using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;

namespace Woopsa
{
    public static class WoopsaMultiRequestConst
    {
        public const string WoopsaMultiRequestMethodName = "MultiRequest";
        public const string WoopsaMultiRequestArgumentName = "Requests";
    }

    public class MultipleRequestResponse
    {
        public int Id { get; set; }
        public string Result { get; set; }
    }

    class Request
    {
        public int Id { get; set; }

        public string Action { get; set; }

        public string Path { get; set; }

        public string Value { get; set; }

        public Dictionary<string, string> Arguments { get; set; }
    }
}
