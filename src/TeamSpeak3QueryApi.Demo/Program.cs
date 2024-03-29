using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Demo;

var loginData = File.ReadAllLines("logindata.secret");

var host = loginData[0].Trim();
var user = loginData[1].Trim();
var password = loginData[2].Trim();

using var rc = new TeamSpeakClient(host);

await rc.Connect();

await rc.Login(user, password);
await rc.UseServer(1);

await rc.WhoAmI();

await rc.RegisterServerNotification();
await rc.RegisterChannelNotification(30);

var serverGroups = await rc.GetServerGroups();
var firstNormalGroup = serverGroups?.FirstOrDefault(s => s.ServerGroupType == ServerGroupType.NormalGroup);
var groupClients = await rc.GetServerGroupClientList(firstNormalGroup.Id);

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
