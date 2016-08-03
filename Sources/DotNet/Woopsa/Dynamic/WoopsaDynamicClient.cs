using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Woopsa
{
    public class WoopsaDynamicClientObject : DynamicObject
    {
        #region Public Override Methods

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            foreach (var property in InnerObject.Properties)
            {
                if (binder.Name.Equals(property.Name))
                {
                    result = property.Value;
                    return true;
                }
            }
            foreach (var item in InnerObject.Items)
            {
                if (binder.Name.Equals(item.Name))
                    if (item is WoopsaBoundClientObject)
                    {
                        result = new WoopsaDynamicClientObject() { InnerObject = (WoopsaBoundClientObject)item };
                        return true;
                    }
            }
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            foreach (var property in InnerObject.Properties)
            {
                if (binder.Name.Equals(property.Name))
                {
                    property.Value = WoopsaValue.ToWoopsaValue(value, property.Type);
                    return true;
                }
            }
            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            foreach (var method in InnerObject.Methods)
            {
                if (method.Name.Equals(binder.Name))
                {
                    var argumentInfos = method.ArgumentInfos.ToArray();
                    var arguments = new IWoopsaValue[argumentInfos.Length];
                    for (int i = 0; i < argumentInfos.Length; i++)
                        arguments[i] = WoopsaValue.ToWoopsaValue(args[i], argumentInfos[i].Type);
                    result = method.Invoke(arguments);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region protected 

        protected WoopsaBoundClientObject InnerObject { get; set; }

        #endregion
    }

    public class WoopsaDynamicClient : WoopsaDynamicClientObject, IDisposable
    {
        public WoopsaDynamicClient(string url)
        {
            _client = new WoopsaClient(url);
            Refresh();
        }

        public string Username
        {
            get { return _client.Username; }
            set { _client.Username = value; }
        }

        public string Password
        {
            get { return _client.Password; }
            set { _client.Password = value; }
        }

        public void Refresh()
        {
            InnerObject = _client.CreateBoundRoot();
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _client.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private readonly WoopsaClient _client;
    }
}
