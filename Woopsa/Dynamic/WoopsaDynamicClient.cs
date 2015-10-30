using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaDynamicClient : DynamicObject, IDisposable
    {
        public WoopsaDynamicClient(string url)
        {
            _client = new WoopsaClient(url);
            _object = new WoopsaDynamicObject(_client.Root);
        }

        public string Username
        {
            get
            {
                return _client.Username;
            }
            set
            {
                _client.Username = value;
            }
        }

        public string Password
        {
            get
            {
                return _client.Password;
            }
            set
            {
                _client.Password = value;
            }
        }

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

        public void Refresh()
        {
            _client.Refresh();
        }

        private WoopsaClient _client;
        private WoopsaDynamicObject _object;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class WoopsaDynamicObject : DynamicObject
    {
        public WoopsaDynamicObject(WoopsaClientObject innerObject)
        {
            _innerObject = innerObject;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            foreach (var property in _innerObject.Properties)
            {
                if (binder.Name.Equals(property.Name))
                {
                    result = ((WoopsaClientProperty)property).Value;
                    return true;
                }
            }
            foreach (var item in _innerObject.Items)
            {
                if (binder.Name.Equals(item.Name))
                {
                    result = new WoopsaDynamicObject((item as WoopsaClientObject));
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
                    property.Value = new WoopsaValue(value.ToWoopsaValue(property.Type), property.Type);
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
                    List<IWoopsaValue> arguments = new List<IWoopsaValue>();
                    for (int i = 0; i < method.ArgumentInfos.Count(); i++)
                    {
                        arguments.Add(args[i].ToWoopsaValue(method.ArgumentInfos.ElementAt(i).Type));
                    }
                    result = method.Invoke(arguments);
                    return true;
                }
            }
            return false;
        }

        private WoopsaClientObject _innerObject;
    }
}
