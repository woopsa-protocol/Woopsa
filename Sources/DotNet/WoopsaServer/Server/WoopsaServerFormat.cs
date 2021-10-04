using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaServerFormat
    {
        public static string Serialize(this IEnumerable<MultipleRequestResponse> responses)
        {
            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var response in responses)
            {
                if (!first)
                    builder.Append(WoopsaFormat.MultipleElementsSeparator);
                else
                    first = false;
                builder.Append(response.Serialize());
            }
            return string.Format(WoopsaFormat.MultipleElementsFormat, builder.ToString());
        }

        public static string Serialize(this MultipleRequestResponse response)
        {
            return string.Format(WoopsaFormat.MultipleRequestResponseFormat, response.Id, response.Result);
        }
    }
}
