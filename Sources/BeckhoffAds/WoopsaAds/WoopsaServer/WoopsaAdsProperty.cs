using TwinCAT.Ads;
using Woopsa;

namespace WoopsaAds
{
    public class WoopsaAdsProperty : WoopsaProperty
    {
        public string RootName { get; private set; }
        public TcAdsSymbolInfo AdsInfo { get; private set; }
        public WoopsaAdsProperty(WoopsaObject container, string name, WoopsaValueType type, WoopsaPropertyGet get, TcAdsSymbolInfo adsInfo) :
             this(container, name, type, get, null, adsInfo)
        {
            
        }
        public WoopsaAdsProperty(WoopsaObject container, string name, WoopsaValueType type, WoopsaPropertyGet get, WoopsaPropertySet set, TcAdsSymbolInfo adsInfo) :
            base(container, name, type, get, set)
        {
            string[] path = adsInfo.Name.Split('.');
            RootName = path[0];
            AdsInfo = adsInfo;
        }
    }
}
