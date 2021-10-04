using System;
using System.Linq;

namespace Woopsa
{
    public class WoopsaBoundClientObject : WoopsaBaseClientObject
    {
        #region Constructors

        internal WoopsaBoundClientObject(WoopsaClient client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(client, container, name, root)
        {
        }

        #endregion

        #region Public Methods

        public void Refresh()
        {
            Clear();
        }

        #endregion

        #region Override PopulateObject

        protected override void PopulateObject()
        {
            WoopsaMetaResult meta;
            base.PopulateObject();

            meta = Client.ClientProtocol.Meta(this.GetPath(Root));
            // Create properties
            if (meta.Properties != null)
                foreach (WoopsaPropertyMeta property in meta.Properties)
                    CreateProperty(
                        property.Name,
                            (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), property.Type),
                            property.IsReadOnly);
            // Create methods
            if (meta.Methods != null)
                foreach (WoopsaMethodMeta method in meta.Methods)
                {
                    var argumentInfos = method.ArgumentInfos.Select(argumentInfo => new WoopsaMethodArgumentInfo(argumentInfo.Name,
                        (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType),
                        argumentInfo.Type))).ToArray();
                    CreateMethod(method.Name, argumentInfos, (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), method.ReturnType));
                }
            // Create items
            if (meta.Items != null)
                foreach (string item in meta.Items)
                {
                    new WoopsaBoundClientObject(Client, this, item, Root);
                }
        }

        #endregion
    }
}