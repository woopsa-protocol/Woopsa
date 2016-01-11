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

    [WoopsaVisibility(WoopsaObjectAdapterVisibility.Declared)]
    public class WeatherStation
    {
        public double Temperature { get; private set; }

        public bool IsRaining { get; set; }

        public int Altitude { get; set; }

        public double Sensitivity { get; set; }

        public string City { get; set; }

        public DateTime Time { get; set; }

        public TimeSpan TimeSinceLastRain { get; set; }

        public Thermostat Thermostat { get; private set; }

        public WeatherStation()
        {
            Temperature = 24.2;
            IsRaining = false;
            Altitude = 430;
            Sensitivity = 0.5;
            City = "Geneva";
            Time = DateTime.Now;
            TimeSinceLastRain = TimeSpan.FromDays(3);
            Thermostat = new Thermostat();
        }

        public string GetWeatherAtDate(DateTime date)
        {
            switch(date.DayOfWeek)
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
                WoopsaServer woopsaServer = new WoopsaServer(root, 80);

                Console.WriteLine("Woopsa server listening on http://localhost:{0}{1}", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);
                Console.WriteLine("Some examples of what you can do directly from your browser:");
                Console.WriteLine(" * View the object hierarchy of the root object:");
                Console.WriteLine("   http://localhost:{0}{1}meta/", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);
                Console.WriteLine(" * Read the value of a property:");
                Console.WriteLine("   http://localhost:{0}{1}read/Temperature", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);
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
