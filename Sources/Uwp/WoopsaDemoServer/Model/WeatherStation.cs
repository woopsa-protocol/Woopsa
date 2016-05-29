using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woopsa;

namespace WoopsaDemoServer
{
    [WoopsaVisibility(WoopsaVisibility.DefaultVisible)]
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
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return "rainy";
                default:
                    return "sunny";
            }
        }
    }
}
