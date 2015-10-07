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
            
            // Display all properties and their values
            Console.WriteLine("Properties and their values:");
            foreach (WoopsaClientProperty property in client.Root.Properties)
            {
                Console.WriteLine(" * {0} : {1} = {2}", property.Name, property.Type, property.Value);

                // As an example, if we find a PropertyInteger value (like in the demo server),
                // we change its value to 1. That way you can see how it's done using the
                // standard client
                if (property.Name == "PropertyInteger")
                {
                    // Create a subscription for example's sake
                    Console.WriteLine("  => Creating a subscription");
                    property.Change += property_Change;

                    // Actually change the value
                    Console.WriteLine("  => Changing value to 1");
                    property.Value = new WoopsaValue(1);
                }
            }

            // Display methods and their arguments
            Console.WriteLine("Methods and their arguments:");
            foreach (WoopsaMethod method in client.Root.Methods)
            {
                // Display the method
                Console.WriteLine(" * {0} : {1}", method.Name, method.ReturnType);
                foreach (WoopsaMethodArgumentInfo argumentInfo in method.ArgumentInfos)
                {
                    Console.WriteLine("  * {0} : {1}", argumentInfo.Name, argumentInfo.Type);
                }

                // As an example, if we find a SayHello method (like in the demo server),
                // we call it. That way you can see how to call methods using the standard
                // client!
                if (method.Name == "SayHello")
                {
                    Console.WriteLine("  => SayHello found! Calling it now...");
					Console.WriteLine("  => Result = {0}", method.Invoke(new List<IWoopsaValue>()
						{
							new WoopsaValue("Woopsa Demo Client")
						})
					);
                }
            }

            // Display embedded items and display its properties
            Console.WriteLine("Items:");
            foreach (WoopsaClientObject item in client.Root.Items)
            {
                // Display the item
                Console.WriteLine(" * {0}", item.Name);

                foreach(WoopsaClientProperty property in item.Properties)
                {
                    Console.WriteLine("  * {0} : {1} = {2}", property.Name, property.Type, property.Value);
                }
            }

            // Leave the DOS window open
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

            // Exit gracefully
            client.Dispose();
        }

        static void property_Change(object sender, WoopsaNotificationEventArgs e)
        {
            Console.WriteLine("Property {0} has changed, new value = {1}", (sender as WoopsaClientProperty).Name, e.Value);
        }
    }
}
