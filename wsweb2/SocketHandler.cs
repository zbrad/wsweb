using System;
using System.Threading.Tasks;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


namespace wsweb2
{
    public class SocketHandler
    {
        public const int BufferSize = 4096;

        WebSocket socket;
        static readonly Encoding utf8 = Encoding.UTF8;

        SocketHandler(WebSocket socket)
        {
            this.socket = socket;
        }

        async Task EchoLoop()
        {
            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);

            while (this.socket.State == WebSocketState.Open)
            {
                var incoming = await this.socket.ReceiveAsync(seg, CancellationToken.None);

                // show how to use the input and change the response
                var input = utf8.GetString(buffer, 0, incoming.Count);  // convert buffer to string
                var s = $"echo: {input}";            // add echo prefix
                var b = utf8.GetBytes(s);            // create outgoing bytes

                var outgoing = new ArraySegment<byte>(b);
                await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        static async Task Acceptor(HttpContext hc, Func<Task> n)
        {
            if (!hc.WebSockets.IsWebSocketRequest)
                return;

            var socket = await hc.WebSockets.AcceptWebSocketAsync();
            var h = new SocketHandler(socket);
            await h.EchoLoop();
        }

        /// <summary>
        /// branches the request pipeline for this SocketHandler usage
        /// </summary>
        /// <param name="app"></param>
        public static void Map(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(SocketHandler.Acceptor);
        }
    }
}
