using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woopsa;
using System.Threading;
using System.Collections.Specialized;
using System.Diagnostics;

namespace WoopsaDemo
{
    public class InnerObject
    {
        public bool Boolean { get; set; }
    }

    public class TestObject
    {
        public TestObject()
        {
            Boolean = true;
            Integer = 10;
            Real = 3.14;
            Text = "Hello World";
            DateTime = DateTime.Now;
            TimeSpan = new TimeSpan(0, 1, 0);

            InnerObject = new InnerObject();
        }

        [WoopsaVisible(true)]
        public bool Boolean { get; private set; }

        [WoopsaVisible(true)]
        public int Integer { get; set; }

        public double Real { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }

        public InnerObject InnerObject { get; set; }

        public void VoidMethod()
        {
            Console.WriteLine("VoidMethod() has been called");
        }

        public int GetDay(DateTime date)
        {
            return date.Day;
        }

        public string SayHello(string name)
        {
            if (name.ToLower() == "florian")
                return "Hello, creator!";
            else if (name.ToLower() == "francois")
                return "Hello, boss!";
            else
                return "Hello, " + name + "!";
        }

        public DateTime GetDate()
        {
            return DateTime.Now;
        }

        public object GetMinute(DateTime dateTime)
        {
            return new InnerObject();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var root = new TestObject();
            root.Text = "Hello \"world\"\nDoes this work?";
            WoopsaServer woopsaServer = new WoopsaServer(root, "/woopsa/");
            woopsaServer.WebServer.Routes.Add("/demo", HTTPMethod.GET, new RouteHandlerFileSystem("..\\..\\Web"));

            WoopsaRoot test = new WoopsaRoot();
            Console.WriteLine("Path = " + test.GetPath());


            WoopsaClient client2 = new WoopsaClient("http://localhost/woopsa");

            dynamic client = new WoopsaDynamicClient("http://localhost/woopsa");
            Console.WriteLine((int)client.Integer + 1);
            client.Text.Change += new EventHandler<WoopsaNotificationEventArgs>(Program_Change);
            Console.WriteLine(client.SayHello("Florian").ToString());

            client2.Root.Properties.ByName("Real").Value = new WoopsaValue(0.5);
            Console.WriteLine("Value = {0}", client2.Root.Properties.ByName("Real").Value);

            client2.Root.Properties.ByName("Real").Change +=Program_Change;

            string sss = new WoopsaValue(DateTime.Now).AsText;
            Console.WriteLine(sss);
            Console.WriteLine("Day = " + client2.Root.Methods.ByName("GetDay").Invoke(new List<WoopsaValue>() { new WoopsaValue(DateTime.Now) }));

            while (true)
            {
                var command = Console.ReadLine();
                if (command.Equals("refresh"))
                {
                    client.Refresh();
                }
                else
                {
                    root.Text = command;
                }
            }
        }

        static void Program_Change(object sender, WoopsaNotificationEventArgs e)
        {
            Console.WriteLine("Change! " + (sender as WoopsaClientProperty).Name + " = " + e.Value.AsText + " at " + e.Value.TimeStamp);
        }
    }
}
