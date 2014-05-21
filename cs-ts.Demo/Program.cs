using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CsTs.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            DoIt();
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        static async void DoIt()
        {
            var cl = new TeamSpeakClient("localhost", 9002);
            await cl.Connect();
            //await cl.Send("use", new Parameter("sid", 1));
            await cl.Send("use", new[] { "sid", "1" });
            await cl.Send("whoami");
            Console.WriteLine("Done1");
            // await cl.Send("login");
        }
    }
}
