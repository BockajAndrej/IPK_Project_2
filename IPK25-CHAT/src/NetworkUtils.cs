using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class NetworkUtils
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    public IPAddress[] ResolveDomain(string serverUrl)
    {
        IPAddress[] addresses = Dns.GetHostAddresses(serverUrl);
        if (addresses.Length == 0)
        {
            throw new Exception("Could not resolve domain to an IP address.");
        }
        return addresses;
    }

    public async Task Connect(ProgProperty prop)
    {
        IPAddress[] serverIp = ResolveDomain(prop.Url);
        
        Debug.WriteLine("Connecting...");

        _client = new TcpClient();
        await _client.ConnectAsync(serverIp, prop.Port);
        
        Debug.WriteLine("Connected to server.");
        
        _stream = _client.GetStream();
    }
    
    public async Task Send(string msg)
    {
        // Send message
        Debug.WriteLine("Sending message.");
        byte[] dataToSend = Encoding.UTF8.GetBytes(msg);
        await _stream!.WriteAsync(dataToSend);
        Debug.WriteLine("Message sent.");
    }

    public async Task<string> Receive(CancellationToken token)
    {
        // Receive response
        byte[] buffer = new byte[1024];
        Debug.WriteLine($"Response.... wait");
        int bytesRead = await _stream!.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Debug.WriteLine($"Response: {response}");
        return response;
    }

    public void Disconnect()
    {
        if (_stream != null) 
            _stream.Close();
        _client?.Close();
    }
}