using System;
using System.Diagnostics;
using System.IO;

namespace TeamSpeak3QueryApi.Net.Demo
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

            using (var cl = new QueryClient(host))
            {
                await cl.Connect();
                await cl.Send("login", new Parameter("client_login_name", user), new Parameter("client_login_password", password));
                await cl.Send("use", new Parameter("sid", 1));
                await cl.Send("whoami");

                //await cl.Send("servernotifyregister", new[] { "event", "channel" }, new[] { "id", "24" });
                await cl.Send("servernotifyregister", new Parameter("event", "channel"), new Parameter("id", "24"));

                cl.Subscribe("clientmoved", data =>
                                            {
                                                Console.WriteLine("Some client moved!");
                                                cl.Unsubscribe("clientmoved");
                                                cl.Send("servernotifyunregister", new[] { "event", "channel" }, new[] { "id", "24" });
                                            });
                // cl.Unsubscribe("message");
            }
            Console.WriteLine("Done1");
        }
    }
}
