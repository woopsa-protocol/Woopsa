using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Threading;

namespace Woopsa
{
    public class WebServer : IDisposable
    {
        #region Constructor

        public WebServer(int port = DefaultPort, bool enableSsl = false)
        {
            _port = port;
            _urls = $"http{(enableSsl ? "s" : string.Empty)}://::{_port};http{(enableSsl ? "s" : string.Empty)}://0.0.0.0:{_port}";
            _endPoints = new List<Endpoint>();
            BaseAddress = $"http{(enableSsl ? "s" : string.Empty)}://localhost:{_port}";
        }

        public WebServer(object root, string routePrefix = EndpointWoopsa.DefaultServerPrefix, int port = DefaultPort, bool enableSsl = false) :
            this(CreateAdapter(root), routePrefix, port, enableSsl)
        {
        }

        public WebServer(WoopsaObject root, string routePrefix = EndpointWoopsa.DefaultServerPrefix, int port = DefaultPort, bool enableSsl = false) :
            this((WoopsaContainer)root, routePrefix, port, enableSsl)
        {
            _endPoints.Clear();
            AddEndPoint(new EndpointWoopsa(root, routePrefix));
        }

        /// <summary>
        /// Creates an instance of the Woopsa server without using the Reflector. You
        /// will have to create the object hierarchy yourself, using WoopsaObjects 
        /// or implementing IWoopsaContainer yourself.
        /// 
        /// It will automatically create the required HTTP server
        /// on the specified port and will prefix woopsa verbs with the specified 
        /// route prefix. It will also add all the necessary native extensions for 
        /// Publish/Subscribe and Mutli-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        /// <param name="port">The port on which to run the web server</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>       
        /// <param name="enableSsl">if true, activate the https protocol and allows secure connections</param>
        public WebServer(IWoopsaContainer root, string routePrefix = EndpointWoopsa.DefaultServerPrefix, int port = DefaultPort, bool enableSsl = false)
            : this(port, enableSsl)
        {
            AddEndPoint(new EndpointWoopsa(root, routePrefix));
        }

        #endregion

        #region Constants

        public const int DefaultPort = 80;
        public const int DefaultPortSsl = 443;

        #endregion

        #region Fields / Attributes

        private IHost _host;
        private int _port = DefaultPort;
        private readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        private readonly List<Endpoint> _endPoints;
        private readonly string _urls;

        #endregion

        #region Properties

        public string BaseAddress { get; }

        public bool IsRunning { get; private set; }

        private BaseAuthenticator _baseAuthenticator;

        public bool Disposed { get; private set; } = false;

        // TODO Assignement du webserver dans endpoint
        private static AsyncLocal<WebServer> _current = new AsyncLocal<WebServer>();
        public static WebServer Current { get { return _current.Value; } }

        /// <summary>
        /// if the value is changed, adds this authentication to all server endpoints.
        /// </summary>
        public BaseAuthenticator Authenticator
        {
            get
            {
                return _baseAuthenticator;
            }
            set
            {
                _baseAuthenticator = value;
                foreach (Endpoint endPoint in _endPoints)
                {
                    endPoint.Authenticator = _baseAuthenticator;
                }
            }
        }

        #endregion

        #region Public Methods

        public void AddEndPoint(Endpoint endpoint)
        {
            if (IsRunning)
            {
                throw new Exception("the web server is already running. " +
                    "It is not possible to add endpoints on the fly. " +
                    "You have to add all the endpoints before starting the web server or stop it and add, then restart it");
            }
            endpoint.SetCurrentWebServer(_port, () => _current.Value = this);
            _endPoints.Add(endpoint);
        }

        public void Start()
        {
            IHostBuilder hostBuilder = CreateHostBuilder(BaseAddress);
            _host = hostBuilder.Build();
            _host.RunAsync();
            IsRunning = true;
        }

        public void Shutdown()
        {
            IsRunning = false;
            _host.StopAsync();
        }

        public void Dispose()
        {
            IsRunning = false;
            Disposed = true;
            _host.Dispose();
            GC.SuppressFinalize(this);
        }

        public void AddX509Certificate2(string certificateLocation, string certificatePassword)
        {
            _certificate = new X509Certificate2(certificateLocation, certificatePassword);
        }

        X509Certificate2 _certificate;

        #endregion

        #region Private Methods

        private IHostBuilder CreateHostBuilder(string baseUrl) =>
            Host.CreateDefaultBuilder()
            .ConfigureLogging(config =>
            {
                config.ClearProviders();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                if (_certificate is not null)
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ConfigureHttpsDefaults(listenOptions =>
                        {
                            // certificate is an X509Certificate2
                            listenOptions.ServerCertificate = _certificate;
                            listenOptions.SslProtocols = SslProtocols.Tls;
                            listenOptions.CheckCertificateRevocation = true;
                        });
                    });
                }
                webBuilder.ConfigureServices(services =>
                {
                    services.AddControllers();

                    services.AddCors(options =>
                    {
                        options.AddPolicy(MyAllowSpecificOrigins,
                            builder =>
                            {
                                builder
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowAnyOrigin();
                            });
                    });
                    services.AddCors();
                })
                .Configure(app =>
                {
                    var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                    if (env.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    else
                    {
                        app.UseStatusCodePages();
                    }
                    app.UseHttpsRedirection();

                    DefaultFilesOptions defaultFileOptions = new DefaultFilesOptions();
                    defaultFileOptions.DefaultFileNames.Clear();
                    defaultFileOptions.DefaultFileNames.Add("index.html");
                    app.UseDefaultFiles(defaultFileOptions);
                    app.UseStaticFiles();
                    app.UseRouting();
                    app.UseCors(MyAllowSpecificOrigins);
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        foreach (Endpoint endpoint in _endPoints)
                        {
                            endpoints.MapWoopsa(endpoint);
                        }

                        endpoints.MapControllers();
                    });
                })
                .UseUrls(_urls);
            });

        private static WoopsaObjectAdapter CreateAdapter(object root)
        {
            return new WoopsaObjectAdapter(null, root.GetType().Name,
                root, null, null, WoopsaObjectAdapterOptions.SendTimestamps,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
        }

        #endregion
    }
}
