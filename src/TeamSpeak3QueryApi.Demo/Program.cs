using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Query.Enums;
using TeamSpeak3QueryApi.Net.Query.Notifications;

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

            // Ssh connection
            //var rc = new TeamSpeakClient(host, 10022, Protocol.SSH);
            //rc.Connect(user, password);

            // Telnet connection
            var rc = new TeamSpeakClient(host);
            await rc.ConnectAsync();
            await rc.LoginAsync(user, password);

            await rc.LoginAsync(user, password);
            await rc.UseServerAsync(1);

            await rc.WhoAmIAsync();

            await rc.RegisterServerNotificationAsync();
            await rc.RegisterAllChannelNotificationAsync();
            
            var serverGroups = await rc.GetServerGroupsAsync();
            var firstNormalGroup = serverGroups?.FirstOrDefault(s => s.ServerGroupType == ServerGroupType.NormalGroup && s.Id != 46); // Id 46 is default Servergroup

            var groupClients = await rc.GetServerGroupClientListAsync(firstNormalGroup.Id);
            var currentClients = await rc.GetClientsAsync();

            var fullClients = currentClients.Where(c => c.Type == ClientType.FullClient).ToList();
            await rc.KickClientAsync(fullClients, KickOrigin.Channel, "You're kicked from channel");

            // await rc.MoveClientAsync(1, 1);
            // await rc.KickClientAsync(1, KickTarget.Server);

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
