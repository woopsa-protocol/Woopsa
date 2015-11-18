using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaJsonDataDynamic : DynamicObject
    {
        public WoopsaJsonDataDynamic(WoopsaJsonData data)
        {
            _data = data;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            try
            {
                if (_data[binder.Name].IsSimple)
                    result = _data[binder.Name];
                else
                    result = new WoopsaJsonDataDynamic(_data[binder.Name]);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            try
            {
                if (_data[(int)indexes[0]].IsSimple)
                    result = _data[(int)indexes[0]];
                else
                    result = new WoopsaJsonDataDynamic(_data[(int)indexes[0]]);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        private WoopsaJsonData _data;
    }

    public static class WoopsaJsonDataDynamicExtensions
    {
        public static WoopsaJsonDataDynamic ToDynamic(this WoopsaJsonData data)
        {
            return new WoopsaJsonDataDynamic(data);
        }
    }
}
