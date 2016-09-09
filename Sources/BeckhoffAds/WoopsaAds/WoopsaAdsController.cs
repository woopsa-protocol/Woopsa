using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using Woopsa;

namespace WoopsaAds
{
    public class WoopsaAdsController
    {
        public bool runAtStartUp;
        public bool isLocal = false;
        public int port = 80; // default port at 80
        public string LOCAL_NET_ID { get { return "127.0.0.1.1.1"; } }
        public string folderPathWebPages;
        public bool isRunning;
        public ObservableCollection<PlcParameter> plcParameterList;

        private volatile bool _shouldStop;
        private List<Thread> _woopsaAdsThreadList;
        private WoopsaServer _woopsaServer;
        private WoopsaObject _rootWoopsaObject;
        private string _configPath;
        private bool _allThreadAreAbort = true;
        private object _thisLock = new object();

        public WoopsaAdsController()
        {
            string defaultLocation = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            Directory.CreateDirectory(defaultLocation + "\\Woopsa");
            _configPath = defaultLocation + "\\Woopsa//WoopsaAdsConfig.xml";
            folderPathWebPages = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Web";
            _woopsaAdsThreadList = new List<Thread>();
            plcParameterList = new ObservableCollection<PlcParameter>();
            LoadConfig();
        }

        public void Start(object argsObject)
        {
            PlcParameter plcParameter = argsObject as PlcParameter;
            try
            {
                WoopsaAdsServer serverConnection = new WoopsaAdsServer(plcParameter.adsNetId);
                WoopsaProperty propertyIsConnected;
                WoopsaObject plc = null;
                bool isWorking = false;
                bool shouldShutDown = false;

                lock (_rootWoopsaObject)
                {
                    DiagnosticWindow.AddPlcStatus(new PlcStatus(plcParameter.name, false, "Starting up"));
                    propertyIsConnected = new WoopsaProperty(_rootWoopsaObject, "IsAlive" + plcParameter.name, WoopsaValueType.Logical, (property) => serverConnection.isAdsConnected);
                }


                while (!shouldShutDown)
                {
                    if (serverConnection.IsHeartBeatAlive())
                    {
                        serverConnection.isAdsConnected = true;
                        if (!serverConnection.isHierarchieLoaded)
                        {
                            if (plc != null)
                            {
                                plc.Dispose();
                            }
                            lock (_rootWoopsaObject)
                            {
                                plc = new WoopsaObject(_rootWoopsaObject, plcParameter.name);
                            }
                            serverConnection.loadHierarchy(plc);
                        }
                        if (!isWorking)
                        {
                            isWorking = true;
                            DiagnosticWindow.AddToDebug(Thread.CurrentThread.Name + " thread  : working...");
                            lock (_rootWoopsaObject)
                            {
                                DiagnosticWindow.PlcStatusChange(plc.Name, isWorking, "Working");
                            }
                        }
                    }
                    else
                    {
                        serverConnection.isAdsConnected = false;
                        serverConnection.isHierarchieLoaded = false;
                        if (plc != null)
                        {
                            plc.Dispose();
                        }
                        DiagnosticWindow.AddToDebug(Thread.CurrentThread.Name + " - Error connection ... Try again");
                        isWorking = false;
                        lock (_rootWoopsaObject)
                        {
                            DiagnosticWindow.PlcStatusChange(plcParameter.name, isWorking, "Error");
                        }
                    }
                    Thread.Sleep(500);
                    lock (plcParameterList)
                    {
                        shouldShutDown = _shouldStop;
                    }
                }
                serverConnection.Dispose();
            }
            catch (SocketException e)
            {
                // A SocketException is caused by an application already listening on a port in 90% of cases
                // Applications known to use port 80:
                //  - On Windows 10, IIS is on by default on some configurations. Disable it here: 
                //    http://stackoverflow.com/questions/30758894/apache-server-xampp-doesnt-run-on-windows-10-port-80
                //  - IIS
                //  - Apache
                //  - Nginx
                //  - Skype

                DiagnosticWindow.AddToDebug("Error: Could not start Woopsa Server. Most likely because an application is already listening on port 80.");
                DiagnosticWindow.AddToDebug("Known culprits:");
                DiagnosticWindow.AddToDebug(" - On Windows 10, IIS is on by default on some configurations.");
                DiagnosticWindow.AddToDebug(" - Skype");
                DiagnosticWindow.AddToDebug(" - Apache, nginx, etc.");
                DiagnosticWindow.AddToDebug("SocketException: " + e.Message);
                MessageBox.Show("SocketException ! : See diagnostic log for more information", "Er ror", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action<Exception>((exc) =>
                    {
                        throw new Exception("Exception from another Thread : " + Thread.CurrentThread.Name, exc);
                    }), ex);
            }
            DiagnosticWindow.AddToDebug(Thread.CurrentThread.Name + " thread: terminating gracefully.");
            lock (_rootWoopsaObject)
            {
                lock (App.appLock)
                {
                    if (App.isExiting)
                        return;
                }
                DiagnosticWindow.PlcStatusChange(plcParameter.name, false, "Stop");
            }
        }

        public void Load()
        {
            _rootWoopsaObject = new WoopsaObject(null, "root");
            _woopsaServer = new WoopsaServer(_rootWoopsaObject, port);
            _woopsaServer.WebServer.Routes.Add("/Pages", HTTPMethod.GET, new RouteHandlerFileSystem(folderPathWebPages));
            Thread thread;

            #region infoDebug
            DiagnosticWindow.AddToDebug("\nWoopsa server listening on http://localhost:" + _woopsaServer.WebServer.Port + _woopsaServer.RoutePrefix);
            DiagnosticWindow.AddToDebug("Some examples of what you can do directly from your browser:");
            DiagnosticWindow.AddToDebug(" * View the object hierarchy of the root object:");
            DiagnosticWindow.AddToDebug("   http://localhost:" + _woopsaServer.WebServer.Port + _woopsaServer.RoutePrefix + "meta/");
            DiagnosticWindow.AddToDebug(" * Read the value of a property:");
            DiagnosticWindow.AddToDebug("   http://localhost:" + _woopsaServer.WebServer.Port + _woopsaServer.RoutePrefix + "read/Temperature ");
            DiagnosticWindow.AddToDebug(" * Surfing on the web pages found in the directory \n   specified in Advanced Settings :");
            DiagnosticWindow.AddToDebug("   http://localhost:" + _woopsaServer.WebServer.Port + "/Pages/ \n");

            #endregion

            bool allThreadAreAbort;
            do
            {
                lock (_thisLock)
                {
                    allThreadAreAbort = _allThreadAreAbort;
                }
            }
            while (!allThreadAreAbort); 
            DiagnosticWindow.ResetPlcStatus();
            _woopsaAdsThreadList = new List<Thread>();
            lock (plcParameterList)
            {
                _shouldStop = false;
            }

            foreach (PlcParameter parameter in plcParameterList)
            {
                thread = new Thread(Start);
                thread.Name = "WoopsaAdsController - " + parameter.name;
                thread.Start(parameter);
                _woopsaAdsThreadList.Add(thread);
            }
            lock (_thisLock)
            {
                _allThreadAreAbort = false;
            }
            isRunning = true;
        }

        private void JoinWoopsaAdsThread(List<Thread> list)
        {
            foreach (Thread thread in list)
            {
                thread.Join();
                thread.Abort();
            }
            lock (_thisLock)
            {
                _allThreadAreAbort = true;
            }
        } 
        public void ShutDown()
        {
            lock (plcParameterList)
            {
                _shouldStop = true;
            }

            // Thread to liberate de mainThread because another thread use Dispatcher.Invoke
            Thread joinWoopsaAdsThread = new Thread(() => JoinWoopsaAdsThread(_woopsaAdsThreadList));
            joinWoopsaAdsThread.Name = "Join WoopsaAdsThread";
            joinWoopsaAdsThread.Start();

            isRunning = false;
            _woopsaServer.Dispose();
            _woopsaServer = null; 
        }

        public void Restart()
        {
            bool isYetShutDown;
            lock (plcParameterList)
            {
                isYetShutDown = _shouldStop;
            }
            if (!isYetShutDown)
                ShutDown();
            SaveConfig(new WoopsaAdsConfig(port, folderPathWebPages, isLocal, runAtStartUp, plcParameterList));

            // Thread to liberate de mainThread because another thread use Dispatcher.Invoke
            Thread load_thread = new Thread(delegate ()
            {
                Load();
            });
            load_thread.Name = "Load thread in Restart()";
            load_thread.Start();
        }

        private void SaveConfig(WoopsaAdsConfig config)
        {
            XmlSerializer writer = new XmlSerializer(typeof(WoopsaAdsConfig));
            using (StreamWriter wr = new StreamWriter(_configPath))
            {
                writer.Serialize(wr, config);
            }
        }

        public void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                XmlSerializer reader = new XmlSerializer(typeof(WoopsaAdsConfig));
                StreamReader file = new StreamReader(_configPath);
                WoopsaAdsConfig config = (WoopsaAdsConfig)reader.Deserialize(file);
                port = config.port;
                folderPathWebPages = config.folderPathWebPages;
                isLocal = config.isLocal;
                runAtStartUp = config.runAtStartUp;
                plcParameterList = config.plcParameterList;
                file.Close();
            }
            else
            {
                SaveConfig(new WoopsaAdsConfig(port, folderPathWebPages, isLocal, runAtStartUp, plcParameterList));
                plcParameterList.Insert(0, new PlcParameter("local plc", LOCAL_NET_ID));
            }           
        }
    }
}
