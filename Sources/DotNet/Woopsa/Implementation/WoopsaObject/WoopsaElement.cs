using System;

namespace Woopsa
{
    public abstract class WoopsaElement : IWoopsaElement, IDisposable
    {
        #region Constructors

        protected WoopsaElement(WoopsaContainer owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        #endregion

        #region IWoopsaElement

        public WoopsaContainer Owner { get; }

        IWoopsaContainer IWoopsaElement.Owner => Owner;

        public string Name { get; }

        #endregion IWoopsaElement

        #region Fields / Attributes

        private bool _isDisposed;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region Protected Methods

        protected void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetPath());
        }

        #endregion
    }
}