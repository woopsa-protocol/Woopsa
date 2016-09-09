
namespace WoopsaAds
{
    public class PlcStatus
    {
        public string plcName { get; set; }

        public bool status { get; set; }

        public string statusName { get; set; }

        public string statusLedPath
        {
            get
            {
                if (status)
                {
                    return "/Images/GreenLed.png";
                }
                else
                {
                    if(statusName == "Stop")
                        return "/Images/YellowLed.png";
                    else
                        return "/Images/RedLed.png";
                }
            }
        }

        public PlcStatus(string name, bool status,string statusName)
        {
            plcName = name;
            this.status = status;
            this.statusName = statusName;
        }
    }
}
