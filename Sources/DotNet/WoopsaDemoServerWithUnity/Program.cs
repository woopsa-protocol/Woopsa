using System;
using System.Threading;
using Woopsa;

namespace WoopsaDemoServerWithUnity
{
    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible)]
    public class DemoMachine
    {
        public string MachineName { get; set; }

        public bool IsRunning { get; set; }
    }

    /// <summary>
    /// This project is used to test the notification system with Woopsa for Unity. 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            _root = new DemoMachine();
            bool done = false;
            using (WebServer woopsaServer = new WebServer(_root, port:2705))
            {
                woopsaServer.Start();
                Console.WriteLine("Woopsa server listening on {0}", woopsaServer.BaseAddress);

                Console.WriteLine();
                Console.WriteLine("Commands : START, STOP, PERF, QUIT");
                do
                {
                    Console.Write(">");
                    switch (Console.ReadLine().ToUpper())
                    {
                        case "QUIT":
                            done = true;
                            break;
                        case "START":
                            _root.IsRunning = true;
                            Console.WriteLine("Started demo machine");
                            break;
                        case "STOP":
                            _root.IsRunning = false;
                            Console.WriteLine("Stopped demo machine");
                            break;
                        case "PERF":
                            StartSubscriptionPerfomanceTest();
                            break;
                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
                while (!done);
            }
        }

        private static DemoMachine _root;
        private static Thread _testPerformanceThread;
        private static CancellationTokenSource _cancellationTokenSource;

        private static void StartSubscriptionPerfomanceTest()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _testPerformanceThread = new Thread(TestSubscriptionPerformance)
            {
                Priority = ThreadPriority.Highest,
                Name = "Test Performance Thread"
            };

            Console.Write("Set sleep delay (in Ms) : ");
            int sleepDelay = int.Parse(Console.ReadLine());

            _testPerformanceThread.Start(sleepDelay);

            Console.WriteLine("Press any key to exit performance test...");
            Console.ReadKey();

            _cancellationTokenSource.Cancel();
            _testPerformanceThread.Join();
        }

        private static void TestSubscriptionPerformance(object param)
        {
            int sleepDelay = (int)param;
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            while(!cancellationToken.IsCancellationRequested)
            {
                _root.IsRunning = true;
                Thread.Sleep(sleepDelay);
                _root.IsRunning = false;
                Thread.Sleep(sleepDelay);
            }
        }
    }
}
