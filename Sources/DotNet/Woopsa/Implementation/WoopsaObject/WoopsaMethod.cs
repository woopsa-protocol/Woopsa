using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaMethod : WoopsaElement, IWoopsaMethod
    {
        #region Constructors

        public WoopsaMethod(WoopsaObject container, string name, WoopsaValueType returnType, IEnumerable<WoopsaMethodArgumentInfo> argumentInfos, WoopsaMethodInvoke methodInvoke)
            : base(container, name)
        {
            ReturnType = returnType;
            ArgumentInfos = argumentInfos;
            _methodInvoke = methodInvoke;
            if (container != null)
                container.Add(this);
        }

        public WoopsaMethod(WoopsaObject container, string name, WoopsaValueType returnType, IEnumerable<WoopsaMethodArgumentInfo> argumentInfos, WoopsaMethodInvoke methodInvoke, WoopsaMethodInvokeAsync methodInvokeAsync)
            : this(container, name, returnType, argumentInfos, methodInvoke)
        {
            _methodInvokeAsync = methodInvokeAsync;
        }

        #endregion

        #region IWoopsaMethod

        public WoopsaValue Invoke(params WoopsaValue[] arguments)
        {
            CheckDisposed();
            return _methodInvoke(arguments);
        }

        public async Task<WoopsaValue> InvokeAsync(params WoopsaValue[] arguments)
        {
            CheckDisposed();
            return await _methodInvokeAsync(arguments);
        }

        public WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments)
        {
            CheckDisposed();
            return _methodInvoke(arguments.ToArray());
        }

        public async Task<WoopsaValue> InvokeAsync(IEnumerable<IWoopsaValue> arguments)
        {
            CheckDisposed();
            return await _methodInvokeAsync(arguments.ToArray());
        }

        IWoopsaValue IWoopsaMethod.Invoke(IWoopsaValue[] arguments)
        {
            return Invoke(arguments);
        }

        async Task<IWoopsaValue> IWoopsaMethod.InvokeAsync(IWoopsaValue[] arguments)
        {
            return await InvokeAsync(arguments);
        }

        public WoopsaValueType ReturnType { get; }

        public IEnumerable<IWoopsaMethodArgumentInfo> ArgumentInfos { get; }

        #endregion IWoopsaMethod

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (Owner != null)
                ((WoopsaObject)Owner).Remove(this);
            base.Dispose(disposing);
        }

        #endregion

        #region Private Members

        private readonly WoopsaMethodInvoke _methodInvoke;

        private readonly WoopsaMethodInvokeAsync _methodInvokeAsync;

        #endregion
    }
}