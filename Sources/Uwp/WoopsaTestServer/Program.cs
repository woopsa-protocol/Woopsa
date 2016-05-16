using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Woopsa;
using WoopsaTest;

namespace WoopsaTestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TestObjectServer objectServer = new TestObjectServer();
                WoopsaServer woopsaServer = new WoopsaServer(objectServer);

                CancellationToken token = new CancellationToken();
                PeriodicTask.Run(() => objectServer.Votes++, TimeSpan.FromMilliseconds(100), token);

                Console.WriteLine("Woopsa server listening on http://localhost:{0}{1}", woopsaServer.WebServer.Port, woopsaServer.RoutePrefix);
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                token.ThrowIfCancellationRequested();
                woopsaServer.Dispose();
            }
            catch (SocketException e)
            {
                Console.WriteLine("Error: Could not start Woopsa Server. Most likely because an application is already listening on port 80.");
                Console.WriteLine("SocketException: {0}", e.Message);
                Console.ReadLine();
            }
        }
    }

    class PeriodicTask
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                    action();
            }
        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}
