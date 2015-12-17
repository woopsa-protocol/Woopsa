using System;
using System.Collections.Generic;
using Woopsa;

namespace WoopsaDemoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(" *** Welcome to the Woopsa Demo Client! *** ");
            Console.WriteLine(" Note: read the source code to understand what's happening behind the scenes!");
            Console.WriteLine("");
            Console.Write("Please enter the Woopsa server URL or leave blank for default (http://localhost/woopsa): ");

            string serverUrl = Console.ReadLine();
            if (serverUrl == "")
                serverUrl = "http://localhost/woopsa";

            WoopsaClient client = new WoopsaClient(serverUrl);
            
            Console.WriteLine("Woopsa client created on URL: {0}", serverUrl);

            ExploreItem(client.Root);

            // Leave the DOS window open
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            client.Dispose();
        }

        static void ExploreItem(WoopsaClientObject obj, int indent = 0)
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
                if (property.Name == "Counter")
                {
                    // Create a subscription for example's sake
                    Console.WriteLine(indentString + "  => Creating a subscription");
                    property.Change += property_Change;

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
            foreach (WoopsaClientObject item in obj.Items)
            {
                ExploreItem(item, indent+1);
            }
        }

        static void property_Change(object sender, WoopsaNotificationEventArgs e)
        {
            Console.WriteLine("Property {0} has changed, new value = {1}", (sender as WoopsaClientProperty).Name, e.Value);
        }
    }
}
