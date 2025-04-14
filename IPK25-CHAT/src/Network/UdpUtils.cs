using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class UdpUtils : ANetUtils
{
    private UdpClient udp;
    private UdpClient udpClient;
    public void Setup()
    {
        udp = new UdpClient();
    }
    public void Send(byte[] data, ProgProperty prop)
    {
        Debug.WriteLine("UDP Sending...");
        IPAddress serverIp = ResolveDomain(prop.Url);
        udp.Send(data, data.Length, serverIp.ToString(), prop.Port);
        Debug.WriteLine("UDP Send!");
    }

    public void SetListenPort(ProgProperty prop)
    {
        udpClient.Dispose();
        udpClient = new UdpClient(prop.Port);
    }

    //Todo: cancelation token missing
    public async Task<byte[]> Receive(CancellationToken token, ProgProperty prop)
    {
        Debug.WriteLine("UDP Receiving...(waiting for data...)");
        UdpReceiveResult result = await udpClient.ReceiveAsync();
        Debug.WriteLine($"UDP Receive: {Encoding.UTF8.GetString(result.Buffer)}");
        return result.Buffer;
    }

    public override void Dispose()
    {
        udpClient.Dispose();
        udp.Dispose();     // lepšia forma (z .NET Core vyššie)
    }
}