using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            var loginData = File.ReadAllLines("..\\..\\..\\logindata.secret");
            var host = loginData[0].Trim();
            var user = loginData[1].Trim();
            var password = loginData[2].Trim();

            var cl = new TeamSpeakClient(host);

            await cl.Connect();
            await cl.Send("login", new[] { "client_login_name", user }, new[] { "client_login_password", password });
            await cl.Send("use", new[] { "sid", "1" }); //await cl.Send("use", new Parameter("sid", 1));
            await cl.Send("whoami");

            cl.Subscribe("message", data => { });
            cl.Unsubscribe("message");

            cl.Disconnect();

            Console.WriteLine("Done1");
        }
    }
}
