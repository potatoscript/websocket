# PotatoWebsocket Project
##### HOME

- [Create the Server](#create_the_server)
- [Create SQLite Database with migration](#create_sqlite_database)
- [Add_Message_Parsing_Real_Time_Updates](#add_message_parsing_real_time_updates)
- [Create_WPF_Client_Connects_to_WebSocket_Server](#create_wpf_client_connects_to_websocket_server)
- [Azure App Service Deployment](#azure_app_service_deployment)

---

## Create_the_Server
##### [home](#home)

### 📁 Folder Structure

```
CadSetupServer/
├── Controllers/
│   └── HealthController.cs
├── Services/
│   └── WebSocketManager.cs
├── appsettings.json
├── Program.cs
└── PotatoServer.csproj
```

### 1. **Create a New Project**

```bash
dotnet new webapi -n PotatoServer
cd PotatoServer
```

---

### 2. **Add WebSocket Support**

#### 📄 `Services/WebSocketManager.cs`

```csharp

using System.Net.WebSockets;
using System.Text;

namespace PotatoServer.Services;

public class WebSocketManager
{
    private static readonly List<WebSocket> _sockets = new();

    public async Task HandleWebSocket(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            _sockets.Add(socket);
            await ReceiveMessages(socket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }

    private async Task ReceiveMessages(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _sockets.Remove(socket);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
            else
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");
                await BroadcastAsync($"Echo: {message}");
            }
        }
    }

    public async Task BroadcastAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        foreach (var socket in _sockets.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
    }
}

```

---

### 3. **Add Health Check API**

#### 📄 `Controllers/HealthController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace PotatoServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Web API is running");
}
```

---

### 4. **Edit `Program.cs` to Add WebSocket Support**

```csharp
using PotatoServer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PotatoServer.Services.WebSocketManager>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

app.MapControllers();

// WebSocket endpoint
app.Map("/ws", async context =>
{
    var wsManager = context.RequestServices.GetRequiredService<PotatoServer.Services.WebSocketManager>();
    await wsManager.HandleWebSocket(context);
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

```

---

### 5. **Optional: SQLite Integration Stub**

You can later add EF Core + SQLite like this:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Let me know if you want full DB setup too.

---

## 🚀 Run Locally

```bash
dotnet run
```

- Web API: `http://localhost:5287/api/health`
- WebSocket: `ws://localhost:5287/ws`

You can test WebSocket using browser dev tools or tools like [[WebSocket King](https://websocketking.com/)](https://websocketking.com/).

---


## ✅ 1. Test the Web API
Since you have Swagger set up (`app.UseSwagger();`), do this:

### 🧪 Open Swagger UI

1. Open your browser.
2. Go to: [http://localhost:5287/swagger](http://localhost:5287/swagger)
3. You’ll see a UI to test your API endpoints. If you’ve defined any controllers, they’ll appear here.

> 📌 If you don’t have any controller yet, create one like this:

```csharp
// Controllers/TestController.cs
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok("Hello from Web API!");
    }
}
```

Then restart and go to:

```
http://localhost:5287/api/test/hello
```

---

## ✅ 2. Test the WebSocket
You exposed WebSocket on `/ws`, so let’s connect to it.

### 🧪 Option A: Test in Browser DevTools

1. Open browser (Chrome is fine)
2. Press `F12` to open DevTools
3. Go to the **Console** tab
4. Paste and run:

```javascript
const socket = new WebSocket("ws://localhost:5287/ws");

socket.onopen = () => {
  console.log("WebSocket connection opened");
  socket.send("Hello WebSocket Server");
};

socket.onmessage = (msg) => {
  console.log("Received:", msg.data);
};

socket.onclose = () => {
  console.log("WebSocket connection closed");
};
```

You should see logs like:

```
WebSocket connection opened
Received: <your echo or message from server>
```

### 🧪 Option B: Use [Postman](https://www.postman.com/) (for WebSocket)

1. Open [Postman](https://www.postman.com/)
2. Click the **"New"** button → WebSocket Request
3. Set URL: `ws://localhost:5287/ws`
4. Click **Connect**
5. Send a message like `"Hello Server"` and check the server's reply


---

##### [home](#home)
## Create_SQLite_Database

#### 1.1 Install SQLite NuGet Package
First, install the SQLite package via NuGet for your Web API project:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

#### 1.2 Create DbContext for SQLite
In your `Services` folder, create a new class `AppDbContext.cs` to manage the SQLite connection and the `EnvSettings` table.

```csharp
using Microsoft.EntityFrameworkCore;

namespace PotatoServer.Services
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<EnvSettings> EnvSettings { get; set; }
    }

    public class EnvSettings
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
```

#### 1.3 Configure DbContext in `Program.cs`
Next, you need to configure the SQLite database in the `Program.cs` file.

```csharp
using PotatoServer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register the DbContext to use SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=envsettings.db")); // The database file path

builder.Services.AddSingleton<PotatoServer.Services.WebSocketManager>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

app.MapControllers();

// WebSocket endpoint
app.Map("/ws", async context =>
{
    var wsManager = context.RequestServices.GetRequiredService<PotatoServer.Services.WebSocketManager>();
    await wsManager.HandleWebSocket(context);
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

```

---

### ✅ Install necessary packages

Run the following command to install the missing package:

```bash
dotnet add package Swashbuckle.AspNetCore
```

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

```bash
dotnet tool install --global dotnet-ef
```

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Then run the migration command again:

```bash
dotnet ef migrations add InitialCreate
```

And after that, update your database with:

```bash
dotnet ef database update
```

---

##### [home](#home)
### Add_Message_Parsing_Real_Time_Updates

Now, let's implement message parsing or filtering logic for WebSockets.

#### 2.1 Create the `WebSocketManager.cs` at your WPF project to Parse Messages

In your `WebSocketManager` class, you can parse and filter incoming messages.

Here's an updated version of `WebSocketManager.cs` to handle different message types:

```csharp

    public class WebSocketManager
    {
        public async Task HandleWebSocket(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await ProcessMessages(webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private async Task ProcessMessages(System.Net.WebSockets.WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var parsedMessage = ParseMessage(message);
                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(parsedMessage)),
                        System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                }
            }
        }

        private string ParseMessage(string message)
        {
            // Example of filtering or parsing logic
            if (message.Contains("command1"))
            {
                return "Command 1 received and processed";
            }
            else if (message.Contains("command2"))
            {
                return "Command 2 received and processed";
            }
            return "Unknown command";
        }
    }
```

This basic filter looks for `command1` and `command2` in the incoming message and sends a response based on the parsed data.

---

##### [home](#home)
### Create_WPF_Client_Connects_to_WebSocket_Server

Now, let's create a simple WPF client that connects to your WebSocket server.

#### 3.1 Set up WPF Project
Create a WPF project in Visual Studio or using the command line:

```bash
dotnet new wpf -n WebSocketClient
cd WebSocketClient
```

#### 3.2 Install WebSocket Client Package
To use WebSockets in a WPF project, you'll need to install the WebSocket client package:

```bash
dotnet add package System.Net.WebSockets.Client
```

#### Create WebSocket Client Code

In your WPF project, create a `WebSocketClient.cs` to manage WebSocket connections.

```csharp
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    public class WebSocketClient
    {
        private ClientWebSocket _webSocket;

        public async Task ConnectAsync(string uri)
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
            await ReceiveMessagesAsync();
        }

        public async Task SendMessageAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received message: " + message);
            }
        }
    }
}
```

#### Connect to WebSocket from WPF UI

In your WPF `MainWindow.xaml.cs`, you can connect to the WebSocket server:

```csharp
using System;
using System.Windows;
using WebSocketClient;

namespace WebSocketClientApp
{
    public partial class MainWindow : Window
    {
        private WebSocketClient _webSocketClient;

        public MainWindow()
        {
            InitializeComponent();
            _webSocketClient = new WebSocketClient();
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            await _webSocketClient.ConnectAsync("ws://localhost:5287/ws");
        }

        private async void ButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            await _webSocketClient.SendMessageAsync("command1");
        }
    }
}
```

#### WPF UI (XAML)

Design your WPF UI in `MainWindow.xaml` to allow users to send and receive messages:

```xml
<Window x:Class="WebSocketClientApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="WebSocket Client" Height="200" Width="300">
    <Grid>
        <Button Name="ButtonConnect" Content="Connect" Width="100" Height="30" VerticalAlignment="Top" HorizontalAlignment="Left" Margin="10" Click="ButtonConnect_Click"/>
        <Button Name="ButtonSendMessage" Content="Send Message" Width="100" Height="30" VerticalAlignment="Top" HorizontalAlignment="Left" Margin="10,50,0,0" Click="ButtonSendMessage_Click"/>
    </Grid>
</Window>
```

---

### Test the Setup

1. **Run the Web API Server**: Run your Web API project with `dotnet run` to ensure the WebSocket server is listening on the correct port (e.g., `ws://localhost:5287/ws`).
   
2. **Run the WPF Client**: Launch the WPF application, and click **Connect** to establish a connection with the WebSocket server. Then, click **Send Message** to send a test message.

You should see the messages being sent and received in both the WebSocket server and the WPF client.

---

### 🔍 Where `Console.WriteLine(...)` goes in a WPF app:
- It goes to the **Output Window** in **Visual Studio**, if you're running the app from there.

---

### ✅ To See It:

1. **Run your WPF app from Visual Studio.**
2. Go to **`View` > `Output`** or press `Ctrl + Alt + O`.
3. Make sure the **dropdown** at the top of the Output window is set to **"Debug"**.
4. You'll see the messages printed there like:
   ```
   Received message: command1
   ```

---

### ⚠️ If You're Running Outside Visual Studio (e.g., `dotnet run`):
You won’t see the output, because WPF apps don’t show a console window by default.

If you really want to see a console **popup alongside the WPF window**, you can do either of these:

#### Option 1: Change Output Type (Temporary)
1. In your WPF `.csproj`, change this line:

   ```xml
   <OutputType>WinExe</OutputType>
   ```

   to:

   ```xml
   <OutputType>Exe</OutputType>
   ```

2. Then when you run the app, a **console window** will open along with your WPF window.

---

##### Azure_App_Service_Deployment
##### [home](#home)
## ☁️ Azure App Service Deployment (Quick Summary)

1. **Create App Service** in Azure Portal:
   - Runtime: .NET 6 or 7
   - Region: Nearby
   - SKU: B1 (Basic) for low cost

2. **Enable WebSockets** in Azure Portal:
   - Go to your App Service → *Configuration* → *General Settings*
   - Set **WebSockets = On**

3. **Publish from Visual Studio**:
   - Right-click project → Publish → Azure → App Service

Or set up **GitHub Actions** for CI/CD.

---

## ✅ Summary

You now have:
- A full working Web API in .NET
- WebSocket support with real-time broadcast
- Azure-ready configuration

---


---

## ✅ What is `dotnet`?

- `dotnet` is the **command-line tool** (CLI) that comes with the **.NET SDK**.
- It allows you to **build**, **run**, and **publish** your .NET applications.
  
Think of it like a helper tool that understands `.NET` apps.

---

## 🔧 Then What is the “Server”?

Your WebSocket application **is the server** — because:

- It **listens for connections** from clients (like your WPF app).
- It **handles messages** (like saving to a database).
- It's built using **ASP.NET Core**, which includes a web server called **Kestrel**.

---

### 🚀 So how does `dotnet` help?

| Tool     | Role |
|----------|------|
| `dotnet` | A **tool** to run your app (`dotnet run` or `dotnet MyApp.dll`) |
| Your WebSocket app | The **server** that handles real-time communication |
| ASP.NET Core | The **framework** powering your server |
| Kestrel | The **web server** built into ASP.NET Core |

---

## 🔁 Example

You created a WebSocket server like this:

```csharp
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await webSocketManager.HandleWebSocket(context);
    }
    else
    {
        await next();
    }
});
```

This is **your server logic**.

You run this app by typing in terminal:

```bash
dotnet run
```

or:

```bash
dotnet MyApp.dll
```

And that starts your **WebSocket server**, not the `dotnet` tool itself.

---

## ✅ In Simple Terms

| You said... | Actually means... |
|-------------|--------------------|
| “Is `dotnet` a server?” | ❌ No — it’s just a command-line tool |
| “What is the real server?” | ✅ Your WebSocket app that runs with ASP.NET Core |
| “Who listens for connections?” | ✅ Your app via `UseWebSockets()` using Kestrel |
| “Why do I need dotnet?” | ✅ To **build** and **run** your app locally or on the server PC |

---

## 💬 TL;DR

🛠 `dotnet` = tool  
🚪 Your WebSocket App = server  
🏗 ASP.NET Core = framework to build it  
🔊 Kestrel = the actual server behind the scenes that handles HTTP/WebSocket connections

---

# 🆚 **Difference Between `Apache`, `IIS`, and `.NET/Kestrel` (your app's server)**

---

## 1️⃣ What They Are

| Server Type | Description |
|-------------|-------------|
| **Apache**  | Open-source **HTTP web server** (mainly used with PHP, Python, static sites) |
| **IIS (Internet Information Services)** | Microsoft’s **web server** built into Windows Server and Windows 10+ |
| **Kestrel** (in .NET) | The **lightweight web server** that runs your **ASP.NET Core apps** (like your WebSocket server) |

---

## 2️⃣ Platform & Language Support

| Server | Best Used With | Cross-platform? |
|--------|----------------|------------------|
| **Apache** | PHP, Python, static files | ✅ Yes (Linux, Windows, macOS) |
| **IIS** | ASP.NET (Classic .NET), .NET Framework apps | ❌ No (Windows only) |
| **Kestrel** | ASP.NET Core apps (Web API, WebSocket) | ✅ Yes (Windows, Linux, macOS) |

---

## 3️⃣ Use Case Comparison

| Feature | Apache | IIS | Kestrel |
|--------|--------|-----|---------|
| **Static file hosting** | ✅ Excellent | ✅ Good | ✅ Good |
| **Dynamic content (.NET)** | ❌ Not for .NET | ✅ (.NET Framework) | ✅ (.NET Core / .NET 6+) |
| **WebSocket support** | 🟡 Yes (via modules) | ✅ Yes | ✅ Native |
| **Reverse proxy needed?** | Often behind Nginx | Often with ARR | Often behind Nginx/IIS in production (but **can run standalone**) |

---

## 4️⃣ Architecture Example

### 🔸 Apache:
- Used in **LAMP stack**: Linux + Apache + MySQL + PHP
- Very common in hosting WordPress, Joomla, etc.

### 🔸 IIS:
- Used in **enterprise Windows environments**
- Hosted apps: ASP.NET Web Forms, Classic MVC, etc.

### 🔸 Kestrel:
- **Modern .NET Core / .NET 6+** apps (like your WebSocket server)
- Lightweight, self-hosted

---

## 5️⃣ Hosting Style

| Server | Requires External Web Server? | Can Run Standalone? |
|--------|-------------------------------|----------------------|
| Apache | No                            | ✅ Yes |
| IIS     | No (built-in hosting)         | ✅ Yes (on Windows) |
| Kestrel | ⚠️ Often needs Nginx/IIS as reverse proxy in production | ✅ Yes (for development / small prod) |

---

## ✅ So for YOU (WebSocket in ASP.NET Core):

- 🟢 **Kestrel is already built into your app**
- ✅ You don’t need Apache or IIS
- 🧠 But in **production** (if you want security + scalability), you can put **Nginx or IIS in front** of Kestrel as a *reverse proxy*

---

## 📝 Summary Table

| Feature               | Apache         | IIS               | Kestrel (ASP.NET Core) |
|-----------------------|----------------|-------------------|-------------------------|
| Platform              | Linux/Windows  | Windows Only      | Cross-platform          |
| .NET support          | ❌ No          | ✅ .NET Framework | ✅ .NET Core/.NET 6+     |
| WebSocket support     | 🟡 Partial     | ✅ Yes            | ✅ Yes                  |
| Reverse Proxy Needed? | Often          | No                | Sometimes (Prod only)   |
| Typical Usage         | PHP/Python     | .NET Framework    | ASP.NET Core apps       |

---

## 💡 What Is a Reverse Proxy?

A **reverse proxy** is like a **middleman** that sits between the **outside world (clients)** and your **actual web app/server** (like Kestrel in .NET Core).

Instead of clients connecting directly to your Kestrel app, they connect to something like:

- **IIS**
- **Nginx**
- **Apache (in some setups)**

…and then **that proxy server** passes the requests back to your Kestrel app **behind the scenes**.

---

## 📥 Without Reverse Proxy (Direct Access)

```
Browser (client)
     │
     ▼
 Kestrel (.NET app runs on port 5000, 7070, etc.)
```

✅ This works fine for **local dev** and **small deployments**.

---

## 🧭 With Reverse Proxy (Production Setup)

```
Browser (client)
     │
     ▼
Nginx / IIS (runs on port 80 or 443 - public web)
     │
     ▼
Kestrel (.NET app runs on port 5000 or similar - private)
```

✅ This is **best practice in production**.

---

## 🛡️ Why Use a Reverse Proxy?

| Benefit | Explanation |
|--------|-------------|
| **Security** | Your Kestrel app is not exposed directly to the internet. Nginx/IIS can handle SSL/TLS (HTTPS), filtering, etc. |
| **Port 80/443** | Only admin/root users can bind to ports 80/443 directly. Kestrel runs on non-privileged ports like 5000. Nginx/IIS handles the public-facing ports. |
| **Load balancing** | A reverse proxy can route traffic to multiple backend apps (scale-out). |
| **Compression & Caching** | Nginx/IIS can compress static files, cache responses, or handle other optimizations. |
| **WebSocket Upgrades** | They can correctly forward WebSocket requests (`Connection: Upgrade`) to your app. |

---

## 🔧 Do You *Need* It Now?

For **your current dev setup (localhost testing with WebSocket + SQLite)**:

**🟢 No — You Don’t Need a Reverse Proxy**

Kestrel is perfectly fine to serve:

```bash
dotnet run
# or runs on http://localhost:5000 or https://localhost:5001
```

---

## 🔜 Later, in Production (example deployment)

| Setup | Description |
|-------|-------------|
| 🧱 IIS | Host your `.NET` app as a background process and use IIS to forward requests |
| 🪞 Nginx | Typical on Linux, acts as reverse proxy to your .NET app |
| ☁️ Cloud | Azure, AWS, etc., often automatically set this up for you |

---

## 📝 TL;DR

> A **reverse proxy** is like a shield that receives requests from clients, and passes them to your app running in the background.





