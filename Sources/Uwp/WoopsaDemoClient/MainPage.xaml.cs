using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Woopsa;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WoopsaDemoClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();


            Task.Factory.StartNew(ManageWoopsa);



        }

        private async void ManageWoopsa()
        {

            string serverUrl = "http://demo.woopsa.org/woopsa";

            WoopsaClient client = new WoopsaClient(serverUrl);
            var o = await client.RefreshAsync();

            ExploreItem(o);


            client.Dispose();
        }


        void ExploreItem(WoopsaClientObject obj, int indent = 0)
        {
            // Display all properties and their values
            string indentString = "";
            for (int i = 0; i < indent; i++)
                indentString += "  ";
            Print(indentString + "{0}", obj.Name);
            Print(indentString + "Properties and their values:");
            foreach (WoopsaClientProperty property in obj.Properties)
            {
                Print(indentString + " * {0} : {1} = {2}", property.Name, property.Type, property.Value);
                if (property.Name == "Altitude")
                {
                    // Create a subscription for example's sake
                    Print(indentString + "  => Creating a subscription");
                    property.Change += property_Change;

                    // Actually change the value
                    Print(indentString + "  => Changing value to 1");
                    property.Value = new WoopsaValue(1);
                }
            }

            // Display methods and their arguments
            Print(indentString + "Methods and their arguments:");
            foreach (WoopsaMethod method in obj.Methods)
            {
                // Display the method
                Print(indentString + " * {0} : {1}", method.Name, method.ReturnType);
                foreach (WoopsaMethodArgumentInfo argumentInfo in method.ArgumentInfos)
                {
                    Print(indentString + "  * {0} : {1}", argumentInfo.Name, argumentInfo.Type);
                }

                // As an example, if we find a SayHello method (like in the demo server),
                // we call it. That way you can see how to call methods using the standard
                // client!
                if (method.Name == "GetWeatherAtDate")
                {
                    Print(indentString + "  => GetWeatherAtDate found! Calling it now...");
                    Print(indentString + "  => Result = {0}", method.Invoke(new List<IWoopsaValue>()
                        {
                            new WoopsaValue(DateTime.Now)
                        })
                    );
                }
            }

            Print(indentString + "Items:");
            foreach (WoopsaClientObject item in obj.Items)
            {
                ExploreItem(item, indent + 1);
            }
        }

        void property_Change(object sender, WoopsaNotificationEventArgs e)
        {
            Print("Property {0} has changed, new value = {1}", (sender as WoopsaClientProperty).Name, e.Value);
        }

        public void Print(string format, params object[] args)
        {
            this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => textbox.Text += string.Format(format, args) + Environment.NewLine);
        }
    }
}
