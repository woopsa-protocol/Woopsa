using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaClientObject : WoopsaObject
    {
        internal WoopsaClientObject(WoopsaBaseClient client, WoopsaClientObject container, string name) 
            : base(container, name)
        {
            _client = client;
            _client.PropertyChange += _client_PropertyChange;
        }

        public delegate void PropertyChanged(IWoopsaValue value);

        public new IEnumerable<WoopsaClientProperty> Properties
        {
            get
            {
                foreach (var property in base.Properties)
                {
                    yield return (property as WoopsaClientProperty);
                }
            }
        }

        public new IEnumerable<WoopsaClientObject> Items
        {
            get
            {
                foreach (var item in base.Items)
                {
                    yield return (item as WoopsaClientObject);
                }
            }
        }

        public void Refresh()
        {
            Refresh(false);
        }

        public void Refresh(bool first)
        {
            _meta = _client.Meta(this.GetPath());

            if (!first)
            {
                for (int i = Properties.Count(); i >= 0; i--)
                {
                    Remove((WoopsaProperty)Properties.ElementAt(i));
                }
                for (int i = Methods.Count(); i >= 0; i--)
                {
                    Remove((WoopsaMethod)Methods.ElementAt(i));
                }
                for (int i = Items.Count(); i >= 0; i--)
                {
                    Remove((WoopsaContainer)Items.ElementAt(i));
                }
            }

            foreach (var property in _meta.Properties)
            {
                if (property.ReadOnly)
                {
                    new WoopsaClientProperty(this,
                        property.Name,
                        (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), property.Type),
                        GetProperty);
                }
                else
                {
                    new WoopsaClientProperty(this,
                        property.Name,
                        (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), property.Type),
                        GetProperty,
                        SetProperty);
                }
            }

            foreach (var method in _meta.Methods)
            {
                List<WoopsaMethodArgumentInfo> argumentInfos = new List<WoopsaMethodArgumentInfo>();
                foreach (var argumentInfo in method.ArgumentInfos)
                    argumentInfos.Add(new WoopsaMethodArgumentInfo(argumentInfo.Name, (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), argumentInfo.Type)));
                new WoopsaMethod(this,
                    method.Name,
                    (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), method.ReturnType),
                    argumentInfos,
                    (args) => (Invoke(args, argumentInfos, method.Name)));
            }

            foreach (var item in _meta.Items)
            {
                new WoopsaClientObject(_client, this, item);
            }
        }

        public void Subscribe(string path, PropertyChanged propertyChangedHandler)
        {
            _client.Subscribe(path);
            _subscriptionsCache.Add(path, propertyChangedHandler);
        }

        public void Unsubscribe(string path)
        {
            if (_subscriptionsCache.ContainsKey(path))
            {
                _subscriptionsCache.Remove(path);
            }
            _client.Unsubscribe(path);
        }

        protected override void PopulateObject()
        {
            base.PopulateObject();
            Refresh(true);
        }

        private void _client_PropertyChange(object sender, WoopsaNotificationsEventArgs notifications)
        {
            foreach (var notification in notifications.Notifications.Notifications)
            {
                if (_subscriptionsCache.ContainsKey(notification.PropertyLink.AsText))
                {
                    _subscriptionsCache[notification.PropertyLink.AsText](notification.Value);
                }
            }
        }

        private WoopsaValue GetProperty(object sender)
        {
            return _client.Read(this.GetPath().TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + (sender as IWoopsaProperty).Name);
        }

        private void SetProperty(object sender, IWoopsaValue value)
        {
            _client.Write(this.GetPath().TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + (sender as IWoopsaProperty).Name, value.AsText);
        }

        private WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments, List<WoopsaMethodArgumentInfo> argumentInfos, string methodName)
        {
            NameValueCollection namedArguments = new NameValueCollection();
            for (int i = 0; i < argumentInfos.Count; i++)
            {
                namedArguments.Add(argumentInfos[i].Name, arguments.ElementAt(i).AsText);
            }
            return _client.Invoke(this.GetPath().TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + methodName, namedArguments);
        }

        private WoopsaBaseClient _client = null;
        private WoopsaMetaResult _meta = null;
        private Dictionary<string, PropertyChanged> _subscriptionsCache = new Dictionary<string, PropertyChanged>();
    }

    public class WoopsaClientProperty : WoopsaProperty
    {
        public WoopsaClientProperty(WoopsaClientObject container, string name, WoopsaValueType type,
            WoopsaPropertyGet get, WoopsaPropertySet set)
            : base(container, name, type, get, set) 
        {
            _container = container;
        }

        public WoopsaClientProperty(WoopsaClientObject container, string name, WoopsaValueType type,
            WoopsaPropertyGet get)
            : this( container, name, type, get, null) { }


        public static implicit operator bool(WoopsaClientProperty property)
        {
            return property.Value.ToBool();
        }

        public static implicit operator sbyte(WoopsaClientProperty property)
        {
            return property.Value.ToSByte();
        }

        public static implicit operator Int16(WoopsaClientProperty property)
        {
            return property.Value.ToInt16();
        }

        public static implicit operator Int32(WoopsaClientProperty property)
        {
            return property.Value.ToInt32();
        }

        public static implicit operator Int64(WoopsaClientProperty property)
        {
            return property.Value.ToInt64();
        }

        public static implicit operator byte(WoopsaClientProperty property)
        {
            return property.Value.ToByte();
        }

        public static implicit operator UInt16(WoopsaClientProperty property)
        {
            return property.Value.ToUInt16();
        }

        public static implicit operator UInt32(WoopsaClientProperty property)
        {
            return property.Value.ToUInt32();
        }

        public static implicit operator UInt64(WoopsaClientProperty property)
        {
            return property.Value.ToUInt64();
        }

        public static implicit operator float(WoopsaClientProperty property)
        {
            return property.Value.ToFloat();
        }

        public static implicit operator double(WoopsaClientProperty property)
        {
            return property.Value.ToDouble();
        }

        public static implicit operator DateTime(WoopsaClientProperty property)
        {
            return property.Value.ToDateTime();
        }

        public static implicit operator TimeSpan(WoopsaClientProperty property)
        {
            return property.Value.ToTimeSpan();
        }

        public static implicit operator string(WoopsaClientProperty property)
        {
            return property.Value.AsText;
        }

        public event EventHandler<WoopsaNotificationEventArgs> Change
        {
            add
            {
                if (!_listenStarted)
                {
                    _container.Subscribe(this.GetPath(), HandlePropertyChanged);
                }
                _changeInternal += value;
            }
            remove
            {
                _changeInternal -= value;
                if (_changeInternal == null)
                {
                    _container.Unsubscribe(this.GetPath());
                }
            }
        }
        private event EventHandler<WoopsaNotificationEventArgs> _changeInternal;

        private void HandlePropertyChanged(IWoopsaValue value)
        {
            if (_changeInternal != null)
            {
                _changeInternal(this, new WoopsaNotificationEventArgs(value));
            }
        }

        private bool _listenStarted = false;
        private WoopsaClientObject _container;
    }

    public class WoopsaNotificationEventArgs : EventArgs
    {
        public WoopsaNotificationEventArgs(IWoopsaValue value)
        {
            Value = value;
        }

        public IWoopsaValue Value { get; set; }
    }

    public static class WoopsaClientExtensions
    {
        public static WoopsaClientProperty ByName(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            WoopsaClientProperty result = ByNameOrNull(properties, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa property not found : {0}", name));
        }

        internal static WoopsaClientProperty ByNameOrNull(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            foreach (var item in properties)
                if (item.Name == name)
                    return item;
            return null;
        }
    }
}
