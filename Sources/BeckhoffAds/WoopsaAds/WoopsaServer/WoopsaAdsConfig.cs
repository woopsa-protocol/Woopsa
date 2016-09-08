using System.Collections.ObjectModel;

namespace WoopsaAds
{
    public class WoopsaAdsConfig
    {
        public int port { get; set; }
        public string folderPathWebPages { get; set; }
        public bool isLocal { get; set; }
        public bool runAtStartUp { get; set; }
        public ObservableCollection<PlcParameter> plcParameterList { get; set; }

        public WoopsaAdsConfig(int port, string folderPathWebPages, bool isLocal, bool runAtStartUp, ObservableCollection<PlcParameter> plcParameterList) 
        {
            this.port = port;
            this.folderPathWebPages = folderPathWebPages;
            this.isLocal = isLocal;
            this.runAtStartUp = runAtStartUp;
            this.plcParameterList = plcParameterList;
        }

        public WoopsaAdsConfig()
        {
            port = 80;
            folderPathWebPages = "";
            isLocal = true;
            runAtStartUp = true;
            plcParameterList = new ObservableCollection<PlcParameter>();
        }
    }
}
