using System;
using Woopsa;

namespace WoopsaDemoServer
{
    public class InnerObject
    {
        public bool PropertyBoolean { get; set; }
    }

    public class TestObject
    {
        public TestObject()
        {
            PropertyBoolean = true;
            PropertyInteger = 10;
            PropertyDouble = 3.14;
            PropertyText = "Hello World";
            PropertyDateTime = DateTime.Now;
            PropertyTimeSpan = new TimeSpan(0, 1, 0);

            InnerObject = new InnerObject();
        }

        public bool PropertyBoolean { get; private set; }
        public int PropertyInteger { get; set; }
        public double PropertyDouble { get; set; }
        public string PropertyText { get; set; }
        public DateTime PropertyDateTime { get; set; }
        public TimeSpan PropertyTimeSpan { get; set; }

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
            return "Hello, " + name + "!";
        }

        public DateTime GetDate()
        {
            return DateTime.Now;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TestObject root = new TestObject();
            WoopsaServer woopsaServer = new WoopsaServer(root);

            Console.Write("Woopsa server listening on http://localhost/woopsa");
        }
    }
}
