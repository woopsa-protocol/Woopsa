namespace WoopsaAds
{
    public class PlcParameter
    {
        public string name { get; set; }
        public string adsNetId { get; set; }

        public PlcParameter(string name, string adsNetId)
        {
            this.name = name;
            this.adsNetId = adsNetId;
        }

        public PlcParameter()
        {
            name = "plc";
            adsNetId = "127.0.0.1.1.1";
        }
    }
}
