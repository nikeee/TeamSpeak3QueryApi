using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;

namespace TeamSpeak3QueryApi.Net.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            DoItRich();
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        static async void DoItRich()
        {
            var loginData = File.ReadAllLines("..\\..\\..\\logindata.secret");

            var host = loginData[0].Trim();
            var user = loginData[1].Trim();
            var password = loginData[2].Trim();

            var rc = new TeamSpeakClient(host);

            await rc.Connect();

            await rc.Login(user, password);
            await rc.UseServer(1);

            await rc.WhoAmI();

            await rc.RegisterServerNotification();
            await rc.RegisterChannelNotification(30);

            // await rc.MoveClient(1, 1);
            // await rc.KickClient(1, KickTarget.Server);

            rc.Subscribe<ClientEnterView>(data => data.ForEach(c => Trace.WriteLine("Client " + c.ClientNickName + " joined.")));
            rc.Subscribe<ClientLeftView>(data => data.ForEach(c => Trace.WriteLine("Client with id " + c.ClientId + " left (kicked/banned/left).")));
            rc.Subscribe<ServerEdited>(data => Debugger.Break());
            rc.Subscribe<ChannelEdited>(data => Debugger.Break());
            rc.Subscribe<ClientMoved>(data => Debugger.Break());

            Console.WriteLine("Done1");
        }

        /*
        static async void DoIt()
        {
            var loginData = File.ReadAllLines("..\\..\\..\\logindata.secret");

            var host = loginData[0].Trim();
            var user = loginData[1].Trim();
            var password = loginData[2].Trim();

            var cl = new QueryClient(host);

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

            Console.WriteLine("Done1");
        }
        */
    }

    internal static class ReadOnlyCollectionExtensions
    {
        public static void ForEach<T>(this IReadOnlyCollection<T> collection, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            foreach (var i in collection)
                action(i);
        }
    }

}
