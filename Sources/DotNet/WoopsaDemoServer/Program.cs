using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
                bool done = false;
                using (WoopsaServer woopsaServer = new WoopsaServer(root, 80))
                {

                    Console.WriteLine("Woopsa server listening on http://localhost:{0}{1}", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);
                    Console.WriteLine("Some examples of what you can do directly from your browser:");
                    Console.WriteLine(" * View the object hierarchy of the root object:");
                    Console.WriteLine("   http://localhost:{0}{1}meta/", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);
                    Console.WriteLine(" * Read the value of a property:");
                    Console.WriteLine("   http://localhost:{0}{1}read/Temperature", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);


                    Console.WriteLine();
                    Console.WriteLine("Commands : QUIT, AUTH, NOAUTH");
                    do
                    {
                        Console.Write(">");
                        switch (Console.ReadLine().ToUpper())
                        {
                            case "QUIT":
                                done = true;
                                break;
                            case "AUTH":
                                woopsaServer.Authenticator = new SimpleAuthenticator(
                                    "WoopsaDemoServer",
                                    (sender, e) => { e.IsAuthenticated = e.Username == "woopsa"; });
                                break;
                            case "NOAUTH":
                                woopsaServer.Authenticator = null;
                                break;
                            default:
                                Console.WriteLine("Invalid command");
                                break;
                        }
                    }
                    while (!done);
                }
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
    }
}
