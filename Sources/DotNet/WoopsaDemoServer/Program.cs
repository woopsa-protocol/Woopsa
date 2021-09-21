using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Threading;
using Woopsa;

namespace WoopsaDemoServer
{
    public class Thermostat
    {
        public double SetPoint { get; set; }

        public Thermostat()
        {
            SetPoint = 20;
        }
    }

    [WoopsaVisibilityAttribute(WoopsaVisibility.DefaultIsVisible)]
    public class WeatherStation
    {
        public double Temperature { get; }

        public bool IsRaining { get; set; }

        public int Altitude { get; set; }

        public double Sensitivity { get; set; }

        public string City { get; set; }

        public DateTime Time => DateTime.Now;

        public TimeSpan TimeSinceLastRain { get; set; }

        public Thermostat Thermostat { get; }

        public WeatherStation()
        {
            Temperature = 24.2;
            IsRaining = false;
            Altitude = 430;
            Sensitivity = 0.5;
            City = "Geneva";
            TimeSinceLastRain = TimeSpan.FromDays(3);
            Thermostat = new Thermostat();
        }

        public string GetWeatherAtDate(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return "rainy";
                default:
                    return "sunny";
            }
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                WeatherStation root = new WeatherStation();
                Thermostat thermostat = new Thermostat();

                //bool done = false;
                //WoopsaServer woopsaServer2 = new WoopsaServer(thermostat, "http://localhost:5099");

                WebServer woopsaServer = new WebServer(root);
                woopsaServer.AddEndPoint(new EndpointWoopsa(thermostat, "tutu"));
                woopsaServer.Start();
                //WoopsaClient woopsaClient = new WoopsaClient("http://localhost/woopsa");

                //woopsaServer.AddNewEndPoint(thermostat, "/woopsa2/");


                //Console.WriteLine("Sensitivity = {0}", woopsaClient.ClientProtocol.Read(nameof(WeatherStation.Sensitivity)));
                //woopsaClient.SubscriptionChannel.Subscribe(nameof(WeatherStation.Sensitivity), Sensitivity_Changed);
                //woopsaClient.CreateBoundRoot();
                //root.Sensitivity = 34;
                //dynamic client = new WoopsaDynamicClient("http://localhost:5100/woopsa");
                ////Reading a property
                //Console.WriteLine("Temperature = {0}", client.Temperature);
                ////Writing a property
                //client.Sensitivity = 0.5;
                ////Invoking a method
                //Console.WriteLine("Weather = {0}", client.GetWeatherAtDate(DateTime.Now));


                //Woopsa.WoopsaServer server = new WoopsaServer(root, port:80);
                //Woopsa.WoopsaServer server2 = new WoopsaServer(thermostat, port:5100);
                Thread.Sleep(3000000);
            }
            catch (SocketException e)
            {
                // A SocketException is caused by an application already listening on a port in most cases
                // Applications known to use port 80:
                //  - On Windows 10, IIS is on by default on some configurations. Disable it here: 
                //    http://stackoverflow.com/questions/30758894/apache-server-xampp-doesnt-run-on-windows-10-port-80
                //  - IIS
                //  - Apache
                //  - Nginx
                //  - Skype
                Console.WriteLine("Error: Could not start Woopsa Server. Most likely because an application is already listening on port 80.");
                Console.WriteLine("Known culprits:");
                Console.WriteLine(" - On Windows 10, IIS is on by default on some configurations.");
                Console.WriteLine(" - Skype");
                Console.WriteLine(" - Apache, nginx, etc.");
                Console.WriteLine("SocketException: {0}", e.Message);
                Console.ReadLine();
            }
        }

        static void ExploreItem(IWoopsaObject obj, int indent = 0)
        {
            // Display all properties and their values
            string indentString = "";
            for (int i = 0; i < indent; i++)
                indentString += "  ";
            Console.WriteLine(indentString + "{0}", obj.Name);
            Console.WriteLine(indentString + "Properties and their values:");
            foreach (WoopsaClientProperty property in obj.Properties)
            {
                Console.WriteLine(indentString + " * {0} : {1} = {2}", property.Name, property.Type, property.Value);
                if (property.Name == "Altitude")
                {
                    // Create a subscription for example's sake
                    Console.WriteLine(indentString + "  => Creating a subscription");
                    property.Subscribe(property_Change);

                    // Actually change the value
                    Console.WriteLine(indentString + "  => Changing value to 1");
                    property.Value = new WoopsaValue(1);
                }
            }

            // Display methods and their arguments
            Console.WriteLine(indentString + "Methods and their arguments:");
            foreach (WoopsaMethod method in obj.Methods)
            {
                // Display the method
                Console.WriteLine(indentString + " * {0} : {1}", method.Name, method.ReturnType);
                foreach (WoopsaMethodArgumentInfo argumentInfo in method.ArgumentInfos)
                {
                    Console.WriteLine(indentString + "  * {0} : {1}", argumentInfo.Name, argumentInfo.Type);
                }

                // As an example, if we find a SayHello method (like in the demo server),
                // we call it. That way you can see how to call methods using the standard
                // client!
                if (method.Name == "GetWeatherAtDate")
                {
                    Console.WriteLine(indentString + "  => GetWeatherAtDate found! Calling it now...");
                    Console.WriteLine(indentString + "  => Result = {0}", method.Invoke(new List<IWoopsaValue>()
                        {
                            new WoopsaValue(DateTime.Now)
                        })
                    );
                }
            }

            Console.WriteLine(indentString + "Items:");
            foreach (WoopsaBoundClientObject item in obj.Items)
            {
                ExploreItem(item, indent + 1);
            }
        }

        static void property_Change(object sender, WoopsaNotificationEventArgs e)
        {
            // TODO : type de sender incorrect
            Console.WriteLine("Property {0} has changed, new value = {1}", (sender as WoopsaClientProperty).Name, e.Notification.Value);
        }
    }
}
