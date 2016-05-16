using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Woopsa
{
    public class WoopsaDynamicClient : DynamicObject, IDisposable
    {
        #region Constructors

        public WoopsaDynamicClient(string url)
        {
            _client = new WoopsaClient(url);
            _object = new WoopsaDynamicClientObject(_client.Root);
        }

        #endregion

        #region Public Properties

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

        #endregion

        #region Public Override Methods

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _object.TryGetMember(binder, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return _object.TrySetMember(binder, value);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            return _object.TryInvokeMember(binder, args, out result);
        }

        public async void RefreshAsync()
        {
            await _client.RefreshAsync();
        }

        #endregion

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

        #region Private Members

        private readonly WoopsaClient _client;
        private readonly WoopsaDynamicClientObject _object;

        #endregion
    }

    public class WoopsaDynamicClientObject : DynamicObject
    {
        #region Constructors

        public WoopsaDynamicClientObject(WoopsaClientObject innerObject)
        {
            _innerObject = innerObject;
        }

        #endregion

        #region Public Override Methods

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            foreach (var property in _innerObject.Properties)
            {
                if (binder.Name.Equals(property.Name))
                {
                    result = property.Value;
                    return true;
                }
            }
            foreach (var item in _innerObject.Items)
            {
                if (binder.Name.Equals(item.Name))
                    if (item is WoopsaClientObject)
                    {
                        result = new WoopsaDynamicClientObject((WoopsaClientObject)item);
                        return true;
                    }
            }
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            foreach (var property in _innerObject.Properties)
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
            foreach (var method in _innerObject.Methods)
            {
                if (method.Name.Equals(binder.Name))
                {
                    var arguments = new List<IWoopsaValue>();
                    for (int i = 0; i < method.ArgumentInfos.Count(); i++)
                        arguments.Add(WoopsaValue.ToWoopsaValue(args[i], method.ArgumentInfos.ElementAt(i).Type));
                    result = method.Invoke(arguments);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Private Members

        private readonly WoopsaClientObject _innerObject;

        #endregion
    }
}
