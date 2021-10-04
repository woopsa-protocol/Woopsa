using System.Collections.Generic;
using System.Linq;

namespace Woopsa
{
    public static class WoopsaClientExtensions
    {
        #region Public Static Methods

        public static WoopsaClientProperty ByName(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            WoopsaClientProperty result = ByNameOrNull(properties, name);

            if (result == null)
                throw new WoopsaNotFoundException(string.Format("Woopsa property not found : {0}", name));

            return result;
        }

        public static WoopsaClientProperty ByNameOrNull(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            return properties.FirstOrDefault(item => item.Name == name);
        }

        #endregion
    }
}