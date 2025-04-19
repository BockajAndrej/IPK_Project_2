using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Network;

public class TcpUtils : ANetUtils
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    
    public override async Task Setup(ProgProperty prop)
    {
        if (prop.Url != null)
        {
            IPAddress serverIp = ResolveDomain(prop.Url);
        
            Debug.WriteLine("Connecting...");

            _client = new TcpClient();
            await _client.ConnectAsync(serverIp, prop.Port);
        }

        Debug.WriteLine("Connected to server.");
        
        _stream = _client?.GetStream();
    }
    
    public override async Task Send(byte[] msg)
    {
        Debug.WriteLine("Sending message.");
        await _stream!.WriteAsync(msg);
        Debug.WriteLine("Message sent.");
    }

    public override async Task<byte[]?> Receive(CancellationToken token)
    {
        byte[] buffer = new byte[1024];
        Debug.WriteLine($"Response.... wait");
        int bytesRead = await _stream!.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
        Debug.WriteLine($"Response: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
        return buffer;
    }

    public override void Dispose()
    {
        if (_stream != null) 
            _stream.Close();
        _client?.Close();
    }
}