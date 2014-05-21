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
            var dict = new Dictionary<string, IParameterValue>();
            dict.Add("sid", (Parameter)"1");
            await cl.Send("use", dict);
            await cl.Send("whoami");
            await cl.Send("login");

        }
    }
}
