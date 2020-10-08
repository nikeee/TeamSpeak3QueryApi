# C# TeamSpeak3Query API [![Travis Build Status](https://travis-ci.org/nikeee/TeamSpeak3QueryAPI.svg?branch=master)](https://travis-ci.org/nikeee/TeamSpeak3QueryAPI) ![NuGet Downloads](https://img.shields.io/nuget/dt/TeamSpeak3QueryApi.svg)

An API wrapper for the TeamSpeak 3 Query API written in C#. **Still work in progress**.

Key features of this library:
- Built entirely with the .NET TAP pattern for perfect async/await usage opportunities
- Robust library architecture
- Query responses are fully mapped to .NET objects, including the naming style
- Usable via Middleware/Rich Client
- SSH and Telnet protocol will be supported

## Contents
1. [Documentation](#documentation)
2. [Compatibility](#compatibility)
  1. [NuGet](#nuget)
3. [Examples](#examples)
  1. [Connect and Login](#connect-and-login)
  2. [Notifications](#notifications)
  3. [Requesting Client Information](#requesting-client-information)
  4. [Exceptions](#exceptions)
4. [Middleware](#middleware)
5. [Node.js](#nodejs)

## Documentation

The TeamSpeak 3 Query API is documented [here](http://media.teamspeak.com/ts3_literature/TeamSpeak%203%20Server%20Query%20Manual.pdf).
This library has an online documentation which was created using [sharpDox](http://sharpdox.de). You can find the documentation on the [GitHub Page of this repository](https://nikeee.github.io/TeamSpeak3QueryAPI).

## Compatibility
This library requires .NET Core `3.0`. You can look at [this table](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support) to see whether your platform is supported. If you find something that is missing (espacially in the `TeamSpeakClient` class), just submit a PR or an issue!

### NuGet
*This is currently not possible.*
```Shell
Install-Package TeamSpeak3QueryApi
# or
dotnet add package TeamSpeak3QueryApi
```

## Examples
Using the rich client, you can connect to a TeamSpeak Query server like this:
### Connect and Login

```C# Telnet query
var rc = new TeamSpeakClient(host, port); // Create rich client instance
await rc.ConnectAsync(); // connect to the server
await rc.LoginAsync(user, password); // login to do some stuff that requires permission
await rc.UseServerAsync(1); // Use the server with id '1'
var me = await rc.WhoAmIAsync(); // Get information about yourself!
```

```C# SSH query
var rc = new TeamSpeakClient(host, 10022, Protocol.SSH); // Create rich client instance
rc.Connect(user, password); // connect to the server with login data
await rc.UseServerAsync(1); // Use the server with id '1'
var me = await rc.WhoAmI(); // Get information about yourself!
```

### Notifications
You can receive notifications. The notification data is fully typed, so you can access the response via properties and not - like other wrappers - using a dictionary.

```C#
// assuming connected
await rc.RegisterServerNotificationAsync(); // register notifications to receive server notifications

// register channel notifications to receive notifications for channel with id '30'
await rc.RegisterChannelNotificationAsync(30);

//Subscribe a callback to a notification:
rc.Subscribe<ClientEnterView>(data => {
    foreach(var c in data)
    {
        Trace.WriteLine("Client " + c.NickName + " joined.");
    }
});
```

### Requesting Client Information
Getting all clients and moving them to a specific channel is as simple as:

```C#
var currentClients = await rc.GetClientsAsync();
await rc.MoveClient(currentClients, 30); // Where 30 is the channel id
```
...and kick someone whose name is "Foobar".

```C#
var fooBar = currentClients.SingleOrDefault(c => c.NickName == "Foobar"); // Using linq to find our dude
if(fooBar != null) // Make sure we pass a valid reference
    await rc.KickClientAsync(fooBar, 30);
```

### Exceptions
There are three exceptions:
- QueryProtocolException

    Only occurs when the server sends an invalid response, meaning the server violates the protocol specifications.
- QueryException

    Occurs every time the server responds with an error code that is not `0`. It holds the error information, for example the error code, error message and - if applicatable - the missing permission id for the operation.
- FileTransferException

    Occurs when there was an error uploading or downloading a file.

Note that exceptions are also thrown when a network problem occurs. Just like a normal TcpClient.

## Middleware
If you want to work more loose-typed, you can do this. This is possible using the `QueryClient`.

```C#
var qc = new QueryClient(host, port);
await qc.ConnectAsync();

await qc.SendAsync("login", new Parameter("client_login_name", userName), new Parameter("client_login_password", password));

await qc.SendAsync("use", new Parameter("sid", "1"));

var me = await qc.SendAsync("whoami");

await qc.SendAsync("servernotifyregister", new Parameter("event", "server"));
await qc.SendAsync("servernotifyregister", new Parameter("event", "channel"), new Parameter("id", channelId));

// and so on.
```
Note that you have to look up the commands in the TeamSpeak documentation.

## Node.js
Suddenly node.

Actually, this library is a port of my TypeScript port of a JS library.

- [The TypeScript port](https://github.com/nikeee/node-ts)
- [The original js library](https://github.com/gwTumm/node-teamspeak)

Note that these ports only contain the (in this library called) middleware.
