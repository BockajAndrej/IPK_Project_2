using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class NetworkUtils
{
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
        int serverPort = prop.Port;
        
        string message = "Hello from client!";
        
        try
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(serverIp, serverPort);
            Console.WriteLine("Connected to server.");

            NetworkStream stream = client.GetStream();

            // Send message
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dataToSend);
            Console.WriteLine("Message sent.");

            // Receive response
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Response: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}