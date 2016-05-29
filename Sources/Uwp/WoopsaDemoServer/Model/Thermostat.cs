using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
