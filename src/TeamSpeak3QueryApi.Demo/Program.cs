using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;

namespace TeamSpeak3QueryApi.Net.Demo
{
    class Program
    {
        static async Task Main(string[] args)
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

            var currentClients = await rc.GetClients();

            var fullClients = currentClients.Where(c => c.Type == ClientType.FullClient).ToList();
            await rc.KickClient(fullClients, KickOrigin.Channel);

            // await rc.MoveClient(1, 1);
            // await rc.KickClient(1, KickTarget.Server);

            rc.Subscribe<ClientEnterView>(data => data.ForEach(c => Debug.WriteLine($"Client {c.NickName} joined.")));
            rc.Subscribe<ClientLeftView>(data => data.ForEach(c => Debug.WriteLine($"Client with id {c.Id} left (kicked/banned/left).")));
            rc.Subscribe<ServerEdited>(data => Debugger.Break());
            rc.Subscribe<ChannelEdited>(data => Debugger.Break());
            rc.Subscribe<ClientMoved>(data => Debugger.Break());

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }

    internal static class ReadOnlyCollectionExtensions
    {
        public static void ForEach<T>(this IReadOnlyCollection<T> collection, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            foreach (var i in collection)
                action(i);
        }
    }
}
