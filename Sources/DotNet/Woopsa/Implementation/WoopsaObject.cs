using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
	public abstract class WoopsaElement : IWoopsaElement, IDisposable
	{
		private WoopsaContainer _container;
		private string _name;

		protected WoopsaElement(WoopsaContainer container, string name)
		{
			_container = container;
			_name = name;
		}

		public WoopsaContainer Container { get { return _container; } }

		#region IWoopsaElement

		public IWoopsaContainer Owner
		{
			get { return _container; }
		}

		public string Name
		{
			get { return _name; }
		}

		#endregion IWoopsaElement

		#region IDisposable

		protected virtual void Dispose(bool disposing)
		{
			_container = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion IDisposable
	}

	public class WoopsaContainer : WoopsaElement, IWoopsaContainer
	{
		public WoopsaContainer(WoopsaContainer container, string name)
			: base(container, name)
		{
			_items = new List<WoopsaContainer>();
			if (Container != null)
				Container.Add(this);
		}

		public IEnumerable<IWoopsaContainer> Items
		{
			get
			{
				DoPopulate();
				foreach (var item in _items)
					yield return item;
			}
		}

		protected virtual void PopulateContainer(IList<WoopsaContainer> items)
		{
		}

		protected void DoPopulate()
		{
			if (!_populated)
			{
				PopulateContainer(_items);
				_populated = true;
			}
		}

		internal void Add(WoopsaContainer item)
		{
            if ( _items.ByNameOrNull(item.Name) != null )
                throw new WoopsaException("Tried to add an item with duplicate name '" + item.Name + "' to WoopsaContainer '" + this.Name + "'");
            _items.Add(item);
		}

		internal void Remove(WoopsaContainer item)
		{
			_items.Remove(item);
		}

		protected override void Dispose(bool disposing)
		{
			if (Container != null)
				Container.Remove(this);
			base.Dispose(disposing);
		}

		private List<WoopsaContainer> _items;
		private bool _populated;

	}

	public delegate WoopsaValue WoopsaPropertyGet(object sender);
	public delegate void WoopsaPropertySet(object sender, IWoopsaValue value);

	public class WoopsaProperty : WoopsaElement, IWoopsaProperty
	{
		public WoopsaProperty(WoopsaObject container, string name, WoopsaValueType type,
			WoopsaPropertyGet get, WoopsaPropertySet set)
			: base(container, name)
		{
			Type = type;
			_get = get;
			IsReadOnly = set == null;
			if (!IsReadOnly)
				_set = set;
			container.Add(this);
		}

		public WoopsaProperty(WoopsaObject container, string name, WoopsaValueType type,
			WoopsaPropertyGet get)
			: this(container, name, type, get, null)
		{
		}

		#region IWoopsaProperty

		public bool IsReadOnly { get; private set; }

		public IWoopsaValue Value
		{
			get { return _get(this); }
			set
			{
				if (!IsReadOnly)
					_set(this, value);
				else
					throw new WoopsaException(String.Format("Cannot set read-only property {0}", Name));
			}
		}

		public WoopsaValueType Type { get; private set; }

		#endregion IWoopsaProperty

		protected override void Dispose(bool disposing)
		{
			((WoopsaObject)Container).Remove(this);
			base.Dispose(disposing);
		}

		private WoopsaPropertyGet _get;
		private WoopsaPropertySet _set;

	}

	public class WoopsaMethodArgumentInfo : IWoopsaMethodArgumentInfo
	{
		public WoopsaMethodArgumentInfo(string name, WoopsaValueType type)
		{
			Name = name;
			Type = type;
		}

		public string Name { get; private set; }

		public WoopsaValueType Type { get; private set; }
	}

	public delegate IWoopsaValue WoopsaMethodInvoke(IEnumerable<IWoopsaValue> Arguments);

	public class WoopsaMethod : WoopsaElement, IWoopsaMethod
	{
		public WoopsaMethod(WoopsaObject container, string name, WoopsaValueType returnType, IEnumerable<WoopsaMethodArgumentInfo> argumentInfos,
			WoopsaMethodInvoke methodInvoke)
			: base(container, name)
		{
			ReturnType = returnType;
			ArgumentInfos = argumentInfos;
			_methodInvoke = methodInvoke;
			container.Add(this);
		}

		#region IWoopsaMethod

		public IWoopsaValue Invoke(IEnumerable<IWoopsaValue> Arguments)
		{
			return _methodInvoke(Arguments);
		}

		public WoopsaValueType ReturnType { get; private set; }

		public IEnumerable<IWoopsaMethodArgumentInfo> ArgumentInfos { get; private set; }

		#endregion IWoopsaMethod

		protected override void Dispose(bool disposing)
		{
			((WoopsaObject)Container).Remove(this);
			base.Dispose(disposing);
		}

		private WoopsaMethodInvoke _methodInvoke;

	}

	public class WoopsaObject : WoopsaContainer, IWoopsaObject
	{
		public WoopsaObject(WoopsaContainer container, string name)
			: base(container, name)
		{
			_properties = new List<WoopsaProperty>();
			_methods = new List<WoopsaMethod>();
		}

		public IEnumerable<IWoopsaProperty> Properties
		{
			get
			{
				DoPopulate();
				return _properties;
			}
		}

		public IEnumerable<IWoopsaMethod> Methods
		{
			get
			{
				DoPopulate();
				return _methods;
			}
		}

		protected override void PopulateContainer(IList<WoopsaContainer> items)
		{
			base.PopulateContainer(items);
			PopulateObject();
		}

		protected virtual void PopulateObject()
		{
		}

		internal void Add(WoopsaProperty item)
		{
            if ( _properties.ByNameOrNull(item.Name) != null )
                throw new WoopsaException("Tried to add a method with duplicate name '" + item.Name + "' to WoopsaObject '" + this.Name + "'");
            _properties.Add(item);
		}

		internal void Remove(WoopsaProperty item)
		{
			_properties.Remove(item);
		}

		internal void Add(WoopsaMethod item)
		{
            if (_methods.ByNameOrNull(item.Name) != null)
                throw new WoopsaException("Tried to add a method with duplicate name '" + item.Name + "' to WoopsaObject '" + this.Name + "'");
            _methods.Add(item);
		}

		internal void Remove(WoopsaMethod item)
		{
			_methods.Remove(item);
		}

		private List<WoopsaProperty> _properties;
		private List<WoopsaMethod> _methods;
	}

	public class WoopsaRoot : WoopsaContainer
	{
		public WoopsaRoot()
			: base(null, string.Empty)
		{
		}
	}
}
