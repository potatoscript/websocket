using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace PotatoServer.Services;

public class WebSocketManager
{
    private static readonly List<WebSocket> _sockets = new();

    // SQLite connection string
    private const string ConnectionString = "Data Source=sqlite.db;Version=3;";

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
