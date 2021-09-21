using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Woopsa
{
    public class ServerRequest
    {
        #region Properties

        public int Id { get; set; }

        public string Verb { get; set; }

        public string Path { get; set; }

        public object Value { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        #endregion
    }
}
