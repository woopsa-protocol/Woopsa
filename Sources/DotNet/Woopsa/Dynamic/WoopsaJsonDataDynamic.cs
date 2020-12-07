using System;
using System.Dynamic;
using System.Text.Json;

namespace Woopsa
{
    public class WoopsaJsonDataDynamic : DynamicObject
    {
        #region Constructors

        public WoopsaJsonDataDynamic(JsonElement data)
        {
            _data = data;
        }

        #endregion

        #region Public Override Methods

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            try
            {
                var property = _data.GetProperty(binder.Name);
                if (property.ValueKind != JsonValueKind.Object)
                    result = property;
                else
                    result = new WoopsaJsonDataDynamic(property);
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
                var property = _data[(int)indexes[0]];
                if (property.ValueKind != JsonValueKind.String)
                    result = property;
                else
                    result = new WoopsaJsonDataDynamic(property);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        #endregion

        #region Private Members

        private readonly JsonElement _data;

        #endregion
    }

    public static class WoopsaJsonDataDynamicExtensions
    {
        public static WoopsaJsonDataDynamic ToDynamic(this JsonElement data)
        {
            return new WoopsaJsonDataDynamic(data);
        }
    }
}
