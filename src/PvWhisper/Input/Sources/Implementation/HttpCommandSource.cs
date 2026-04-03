using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using PvWhisper.Input.Sources;
using PvWhisper.Logging;

namespace PvWhisper.Input.Sources.Implementation;

public sealed class HttpCommandSource : ICommandSource, IDisposable
{
    private readonly HttpListener _listener;
    private readonly ILogger _logger;
    private readonly int _port;

    public HttpCommandSource(int port, ILogger logger)
    {
        _port = port;
        _logger = logger;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    public async IAsyncEnumerable<char> ReadCommandsAsync([EnumeratorCancellation] CancellationToken token)
    {
        _listener.Start();
        _logger.Info($"HTTP command server listening — curl -s -X POST http://localhost:{_port}/command/{{cmd}}");

        var channel = Channel.CreateUnbounded<char>();

        _ = Task.Run(() => AcceptLoopAsync(channel.Writer, token), CancellationToken.None);

        await foreach (var cmd in channel.Reader.ReadAllAsync(token))
            yield return cmd;
    }

    private async Task AcceptLoopAsync(ChannelWriter<char> writer, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().WaitAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                _ = Task.Run(() => HandleRequestAsync(context, writer), CancellationToken.None);
            }
        }
        finally
        {
            writer.TryComplete();
            _listener.Stop();
        }
    }

    private static async Task HandleRequestAsync(HttpListenerContext context, ChannelWriter<char> writer)
    {
        var path = context.Request.Url?.AbsolutePath.Trim('/') ?? "";
        var parts = path.Split('/');

        if (context.Request.HttpMethod == "POST" &&
            parts.Length == 2 &&
            parts[0] == "command" &&
            parts[1].Length == 1)
        {
            writer.TryWrite(parts[1][0]);
            context.Response.StatusCode = 200;
        }
        else
        {
            context.Response.StatusCode = 400;
        }

        await context.Response.OutputStream.FlushAsync();
        context.Response.Close();
    }

    public void Dispose()
    {
        ((IDisposable)_listener).Dispose();
    }
}
