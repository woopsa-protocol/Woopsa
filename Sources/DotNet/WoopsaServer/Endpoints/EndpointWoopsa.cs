using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    public class EndpointWoopsa : Endpoint, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Creates an instance of the Woopsa endpoint adding multiRequestHandler and SubscriptionService
        /// It will automatically prefix woopsa verbs with the specified 
        /// route prefix. It will also add all the necessary native extensions for 
        /// Publish/Subscribe and Mutli-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published.</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>
        public EndpointWoopsa(WoopsaObject root, string routePrefix = DefaultServerPrefix):
            this((IWoopsaContainer)root, routePrefix)
        {
            new WoopsaMultiRequestHandler(root, this);
            _subscriptionService = new WoopsaSubscriptionService(this, root);
            _subscriptionService.CanReconnectSubscriptionToNewObject += OnCanWatch;
        }

        /// <summary>
        /// Creates an instance of the Woopsa endpoint and Publish a new woopsa object 
        /// to the specified route prefix without using the Reflector.
        /// It will automatically prefix woopsa verbs with the specified 
        /// route prefix.
        /// </summary>
        /// <param name="root">The root object that will be published.</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>
        public EndpointWoopsa(IWoopsaContainer root, string routePrefix = DefaultServerPrefix):
            base(routePrefix)
        {
            _root = root;
        }

        /// <summary>
        /// Creates an instance of the Woopsa endpoint and Publish a new woopsa object 
        /// to the specified route prefix without using the Reflector.
        /// It will automatically prefix woopsa verbs with the specified 
        /// route prefix.
        /// </summary>
        /// <param name="root">The root object that will be published.</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>
        public EndpointWoopsa(object root, string routePrefix = DefaultServerPrefix) :
            this(CreateAdapter(root),routePrefix)
        {
        }

        #endregion

        #region Constants

        public const string DefaultServerPrefix = "/woopsa/";

        #endregion

        #region Fields / Attributes

        private WoopsaSubscriptionService _subscriptionService;
        private IWoopsaContainer _root;
        static WoopsaValue[] NoArguments = new WoopsaValue[0];

        private static AsyncLocal<int> _woopsaModelAccessCounter = new AsyncLocal<int>();

        #endregion

        #region Events

        /// <summary>
        /// This event is triggered before WoopsaServer accesses the WoopsaObject model.
        /// It is usefull to protect against concurrent WoopsaObject accesses at a global level.
        /// </summary>
        public event EventHandler BeforeWoopsaModelAccess;

        /// <summary>
        /// This event is triggered after WoopsaServer accesses the WoopsaObject model.
        /// It is guaranteed that for each BeforeWoopsaModelAccess event fired, 
        /// the AfterWoopsaModelAccess event will be fired.
        /// </summary>
        public event EventHandler AfterWoopsaModelAccess;

        #endregion

        #region Public Methods

        public override async Task HandleRequestAsync(HttpContext context, string subRoute)
        {
            try
            {
                context.Response.ContentType = MediaTypeNames.Application.Json;
                (WoopsaVerb, string) woopsaArguments = GetVerbAndWoopsaPath(subRoute, context);
                WoopsaVerb verb = woopsaArguments.Item1;
                string woopsaPath = woopsaArguments.Item2;
                Console.WriteLine(subRoute);
                string result = null;
                using (new WoopsaServerModelAccessLockedSection(this))
                {
                    switch (verb)
                    {
                        case WoopsaVerb.Meta:
                            result = GetMetadata(woopsaPath);
                            break;
                        case WoopsaVerb.Read:
                            result = ReadValue(woopsaPath);
                            break;
                        case WoopsaVerb.Write:
                            result = WriteValue(woopsaPath, context.Request.Form["value"]);
                            break;
                        case WoopsaVerb.Invoke:
                            result = InvokeMethod(woopsaPath, context.Request.Form);
                            break;
                    }
                }
                if (result != null)
                    await context.Response.WriteAsync(result);
            }
            catch (WoopsaNotFoundException e)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, e.Serialize());
                OnHandledException(e);
            }
            catch (WoopsaInvalidOperationException e)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.BadRequest, e.Serialize());
                OnHandledException(e);
            }
            catch (WoopsaException e)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.InternalServerError, e.Serialize());
                OnHandledException(e);
            }
            catch (Exception e)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.InternalServerError, e.Serialize());
                OnHandledException(e);
            }
        }

        /// <summary>
        /// This method simply called BeforeWoopsaModelAccess to trigger concurrent access 
        /// protection over the woopsa model.
        /// </summary>
        /// <returns>
        /// a WoopsaServerModelAccessFreeSection that must be disposed to leave the
        /// Model access-free section
        /// </returns>
        public WoopsaServerModelAccessFreeSection EnterModelAccessFreeSection()
        {
            return new WoopsaServerModelAccessFreeSection(this);
        }

        public void ShutDown()
        {
            if (_subscriptionService is not null)
                _subscriptionService.Terminate();
        }

        #endregion

        #region Protected Methods

        protected internal void ExecuteBeforeWoopsaModelAccess()
        {
            _woopsaModelAccessCounter.Value++;
            if (_woopsaModelAccessCounter.Value == 1)
                OnBeforeWoopsaModelAccess();
        }

        protected internal void ExecuteAfterWoopsaModelAccess()
        {
            _woopsaModelAccessCounter.Value--;
            if (_woopsaModelAccessCounter.Value == 0)
                OnAfterWoopsaModelAccess();
            else if (_woopsaModelAccessCounter.Value < 0)
                throw new WoopsaException("Woopsa internal model lock counting error");
        }

        protected virtual void OnBeforeWoopsaModelAccess()
        {
            if (BeforeWoopsaModelAccess != null)
                BeforeWoopsaModelAccess(this, new EventArgs());
        }

        protected virtual void OnAfterWoopsaModelAccess()
        {
            if (AfterWoopsaModelAccess != null)
                AfterWoopsaModelAccess(this, new EventArgs());
        }

        protected virtual void OnCanWatch(object sender, CanWatchEventArgs e)
        {
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ShutDown();
                if (_subscriptionService is not null)
                {
                    _subscriptionService.Dispose();
                    _subscriptionService = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Core Functions

        internal string ReadValue(string path)
        {
            IWoopsaElement item = FindByPath(path);
            if ((item is IWoopsaProperty))
            {
                IWoopsaProperty property = item as IWoopsaProperty;
                string result = property.Value.Serialize();
                OnLog(WoopsaVerb.Read, path, NoArguments, result, true);
                return result;
            }
            else
            {
                string message = string.Format("Cannot read value of a non-WoopsaProperty for path {0}", path);
                OnLog(WoopsaVerb.Read, path, NoArguments, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        private string WriteValue(string path, Func<WoopsaValueType, WoopsaValue> getValue)
        {
            IWoopsaElement item = FindByPath(path);
            if ((item is IWoopsaProperty))
            {
                IWoopsaProperty property = item as IWoopsaProperty;
                WoopsaValue argument = getValue(property.Type);
                if (!property.IsReadOnly)
                {
                    property.Value = argument;
                    string result = property.Value.Serialize();
                    OnLog(WoopsaVerb.Write, path, new WoopsaValue[] { argument }, result, true);
                    return result;
                }
                else
                {
                    string message = string.Format(
                        "Cannot write a read-only WoopsaProperty for path {0}", path);
                    OnLog(WoopsaVerb.Write, path, new WoopsaValue[] { argument }, message, false);
                    throw new WoopsaInvalidOperationException(message);
                }
            }
            else
            {
                WoopsaValue argument = getValue(WoopsaValueType.Text);
                string message = string.Format("Cannot write value of a non-WoopsaProperty for path {0}", path);
                OnLog(WoopsaVerb.Write, path, new WoopsaValue[] { argument }, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        internal string WriteValue(string path, string value)
        {
            return WriteValue(path, (woopsaValueType) => WoopsaValue.CreateUnchecked(value, woopsaValueType));
        }

        internal string WriteValueDeserializedJson(string path, object deserializedJson)
        {
            return WriteValue(path, (woopsaValueType) => WoopsaValue.DeserializedJsonToWoopsaValue(deserializedJson, woopsaValueType));
        }

        internal string GetMetadata(string path)
        {
            IWoopsaElement item = FindByPath(path);
            if (item is IWoopsaContainer)
            {
                string result;
                if (item is IWoopsaObject)
                    result = (item as IWoopsaObject).SerializeMetadata();
                else
                    result = (item as IWoopsaContainer).SerializeMetadata();
                OnLog(WoopsaVerb.Meta, path, NoArguments, result, true);
                return result;
            }
            else
            {
                string message = string.Format("Cannot get metadata for a WoopsaElement of type {0}", item.GetType());
                OnLog(WoopsaVerb.Meta, path, NoArguments, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        private string InvokeMethod(string path, int argumentsCount,
            Func<string, WoopsaValueType, WoopsaValue> getArgumentByName)
        {
            IWoopsaElement item = FindByPath(path);
            if (item is IWoopsaMethod)
            {
                IWoopsaMethod method = item as IWoopsaMethod;
                if (argumentsCount == method.ArgumentInfos.Count())
                {
                    List<WoopsaValue> woopsaArguments = new List<WoopsaValue>();
                    foreach (var argInfo in method.ArgumentInfos)
                    {
                        WoopsaValue argumentValue = getArgumentByName(argInfo.Name, argInfo.Type);
                        if (argumentValue == null)
                        {
                            string message = string.Format("Missing argument {0} for method {1}", argInfo.Name, item.Name);
                            OnLog(WoopsaVerb.Invoke, path, NoArguments, message, false);
                            throw new WoopsaInvalidOperationException(message);
                        }
                        else
                            woopsaArguments.Add(argumentValue);
                    }
                    WoopsaValue[] argumentsArray = woopsaArguments.ToArray();
                    IWoopsaValue methodResult = method.Invoke(argumentsArray);
                    string result = methodResult != null ? methodResult.Serialize() : WoopsaConst.WoopsaNull;
                    OnLog(WoopsaVerb.Invoke, path, argumentsArray, result, true);
                    return result;
                }
                else
                {
                    string message = string.Format("Wrong argument count for method {0}", item.Name);
                    OnLog(WoopsaVerb.Invoke, path, NoArguments, message, false);
                    throw new WoopsaInvalidOperationException(message);
                }
            }
            else
            {
                string message = string.Format("Cannot invoke a {0}", item.GetType());
                OnLog(WoopsaVerb.Invoke, path, NoArguments, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        internal string InvokeMethodDeserializedJson(string path, Dictionary<string, object> arguments)
        {
            int argumentsCount = arguments != null ? arguments.Count : 0;
            return InvokeMethod(path, argumentsCount,
                (argumentName, woopsaValueType) =>
                {
                    object argumentValue = arguments[argumentName];
                    if (argumentValue != null)
                        return WoopsaValue.DeserializedJsonToWoopsaValue(
                            argumentValue, woopsaValueType);
                    else
                        return null;
                });
        }

        internal string InvokeMethod(string path, IQueryCollection arguments)
        {
            int argumentsCount = arguments != null ? arguments.Count : 0;
            return InvokeMethod(path, argumentsCount,
                (argumentName, woopsaValueType) =>
                {
                    string argumentValue = arguments[argumentName];
                    if (argumentValue != null)
                        return WoopsaValue.CreateChecked(argumentValue, woopsaValueType);
                    else
                        return null;
                });
        }

        internal string InvokeMethod(string path, IFormCollection arguments)
        {
            int argumentsCount = arguments != null ? arguments.Count : 0;
            return InvokeMethod(path, argumentsCount,
                (argumentName, woopsaValueType) =>
                {
                    string argumentValue = arguments[argumentName];
                    if (argumentValue != null)
                        return WoopsaValue.CreateChecked(argumentValue, woopsaValueType);
                    else
                        return null;
                });
        }

        #endregion

        #region Private Members

        private (WoopsaVerb, string) GetVerbAndWoopsaPath(string subRoute, HttpContext context)
        {
            if (subRoute is null)
            {
                throw new WoopsaException(string.Format($"Subroute for the specific {RoutePrefix} is null", context.Request.Method));
            }
            string[] routeItems = subRoute.Split(WoopsaConst.WoopsaPathSeparator, 2);
            WoopsaVerb? woopsaVerb = WoopsaFormat.StringToVerb(routeItems[0]);
            if (woopsaVerb is null)
            {
                throw new WoopsaException(string.Format("Woopsa verb '{0}' is not in a correct format", woopsaVerb, context.Request.Method));
            }
            if (((woopsaVerb == WoopsaVerb.Invoke || woopsaVerb == WoopsaVerb.Write) &&
                    context.Request.Method != WebRequestMethods.Http.Post) ||
                    ((woopsaVerb == WoopsaVerb.Read || woopsaVerb == WoopsaVerb.Meta) &&
                    context.Request.Method != WebRequestMethods.Http.Get))
                throw new WoopsaException(string.Format(
                    "Woopsa verb '{0}' and HTTP method {1} are incompatible",
                    woopsaVerb, context.Request.Method));

            string woopsaPath = routeItems.Length >= 2 ? routeItems[1] : string.Empty;
            return (woopsaVerb.Value, woopsaPath);
        }


        private IWoopsaElement FindByPath(string searchPath)
        {
            return _root.ByPath(searchPath);
        }

        private static WoopsaObjectAdapter CreateAdapter(object root)
        {
            return new WoopsaObjectAdapter(null, root.GetType().Name,
                root, null, null, WoopsaObjectAdapterOptions.SendTimestamps,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
        }

        #endregion
    }
}
