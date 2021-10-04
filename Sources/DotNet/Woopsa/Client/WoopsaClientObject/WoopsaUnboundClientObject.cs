using System;

namespace Woopsa
{
    public class WoopsaUnboundClientObject : WoopsaBaseClientObject
    {
        #region Constructors

        internal WoopsaUnboundClientObject(WoopsaClient client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(client, container, name, root)
        {
        }

        #endregion

        #region public methods

        /// <summary>
        ///     Returns an existing unbound ClientProperty with the corresponding name
        ///     or creates a new one if not found.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="readOnly">
        ///     null means we don't care and want to get back the existing property if any.
        ///     if none is available, a read/write property is then returned.
        /// </param>
        /// <returns></returns>
        public WoopsaClientProperty GetProperty(string name, WoopsaValueType type, bool? readOnly = null)
        {
            WoopsaProperty result = Properties.ByNameOrNull(name);
            if (result != null)
            {
                if (result.Type != type)
                    throw new Exception(string.Format(
                        "A property with then name '{0}' exists, but with the type {1} instead of {2}",
                        name, result.Type, type));
                else if (readOnly != null && result.IsReadOnly != readOnly)
                    throw new Exception(string.Format(
                        "A property with then name '{0}' exists, but with the readonly flag {1} instead of {2}",
                        name, result.IsReadOnly, readOnly));
                else if (!(result is WoopsaClientProperty))
                    throw new Exception(string.Format(
                        "A property with then name '{0}' exists, but it is not of the type WoopsaClientProperty", name));
                else
                    return result as WoopsaClientProperty;
            }
            else
                return base.CreateProperty(name, type, readOnly ?? true);
        }

        /// <summary>
        ///     Returns an existing unbound ClientProperty with the corresponding path
        ///     or creates a new one if not found.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="readOnly">
        ///     null means we don't care and want to get back the existing property if any.
        ///     if none is available, a read/write property is then returned.
        /// </param>
        /// <returns></returns>
        public WoopsaClientProperty GetPropertyByPath(string path, WoopsaValueType type, bool? readOnly = null)
        {
            WoopsaUnboundClientObject container;
            string[] pathParts = path.Split(WoopsaConst.WoopsaPathSeparator);

            if (pathParts.Length > 0)
            {
                container = this;
                for (int i = 0; i < pathParts.Length - 1; i++)
                    container = container.GetUnboundItem(pathParts[i]);
                return container.GetProperty(pathParts[pathParts.Length - 1], type, readOnly);
            }
            else
                throw new Exception(
                    string.Format("The path '{0}' is not valid to referemce a property", path));
        }

        public WoopsaMethod GetMethod(string name, WoopsaValueType returnType,
            WoopsaMethodArgumentInfo[] argumentInfos)
        {
            WoopsaMethod result = Methods.ByNameOrNull(name);
            if (result != null)
            {
                if (result.ReturnType != returnType)
                    throw new Exception(string.Format(
                        "A method with then name {0} exists, but with the return type {1} instead of {2}",
                        name, result.ReturnType, returnType));
                else if (result.ArgumentInfos.IsSame(argumentInfos))
                    throw new Exception(string.Format(
                        "A method with then name {0} exists, but with different arguments",
                        name));
                else
                    return result;
            }
            else
                return base.CreateMethod(name, argumentInfos, returnType);
        }

        //public WoopsaMethod GetAsynchronousMethod(string name, WoopsaValueType returnType,
        //    WoopsaMethodArgumentInfo[] argumentInfos)
        //{
        //    WoopsaMethod result = Methods.ByNameOrNull(name);
        //    if (result != null)
        //    {
        //        if (result.ReturnType != returnType)
        //            throw new Exception(string.Format(
        //                "A method with then name {0} exists, but with the return type {1} instead of {2}",
        //                name, result.ReturnType, returnType));
        //        else if (result.ArgumentInfos.IsSame(argumentInfos))
        //            throw new Exception(string.Format(
        //                "A method with then name {0} exists, but with different arguments",
        //                name));
        //        else
        //            return result;
        //    }
        //    else
        //        return base.CreateMethodAsync(name, argumentInfos, returnType);
        //}

        public WoopsaUnboundClientObject GetUnboundItem(string name)
        {
            return new WoopsaUnboundClientObject(Client, this, name, Root);
        }

        public WoopsaBoundClientObject GetBoundItem(string name)
        {
            return new WoopsaBoundClientObject(Client, this, name, Root);
        }

        #endregion
    }
}