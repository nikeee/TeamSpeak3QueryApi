C# TeamSpeak3Query API
======================

An API wrapper for the TeamSpeak 3 Query API written in C#. Still work in progress.

Key features of this library:
- Built entirely with the .NET TAP pattern for perfect async/await usage opportunities
- Robust library architecture
- Query responses are fully mapped to .NET objects, including the naming style
- Usable via Middleware/Rich Client

# Examples
Using the rich client, you can connect to a TeamSpeak Query server like this:
## Connect and Login
```C#
var rc = new TeamSpeakClient(host, port); // Create rich client instance
await rc.Connect(); // connect to the server
await rc.Login(user, password); // login to do some stuff that requires permission
await rc.UseServer(1); // Use the server with id '1'
var me = await rc.WhoAmI(); // Get information about yourself!
```
## Notifications
You can receive notifications. The notification data is fully typed, so you can access the response via properties and not - like other wrappers - using a dictionary.
```C#
// assuming connected
await rc.RegisterServerNotification(); // register notifications to receive server notifications

// register channel notifications to receive notifications for channel with id '30'
await rc.RegisterChannelNotification(30);

//Subscribe a callback to a notification:
rc.Subscribe<ClientEnterView>(data => {
    foreach(var c in data)
    {
        Trace.WriteLine("Client " + c.ClientNickName + " joined.");
    }
});
```

## Further Operations
Getting all clients and moving them to a specific channel is as simple as:
```C#
var currentClients = await rc.GetClients();
await rc.MoveClient(currentClients, 30); // Where 30 is the channel id
```
...and kick someone whose name is "Foobar".
```C#
var fooBar = currentClients.SingleOrDefault(c => c.ClientNickName == "Foobar"); // Using linq to find our dude
if(fooBar != null) // Make sure we pass a valid reference
    await rc.KickClient(fooBar, 30);
```

## Middleware
If you want to work more loose-typed, you can do this. This is possible using the `QueryClient`.
```C#
var qc = new QueryClient(host, port);
await qc.Connect();

await qc.Send("login", new Parameter("client_login_name", userName), new Parameter("client_login_password", password));

await qc.Send("use", new Parameter("sid", "1"));

var me = await qc.Send("whoami");

await qc.Send("servernotifyregister", new Parameter("event", "server"));
await qc.Send("servernotifyregister", new Parameter("event", "channel"), new Parameter("id", channelId));

// and so on.
```
Note that you have to look up the commands in the TeamSpeak documentation.