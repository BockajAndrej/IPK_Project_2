using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Network;

public class UdpUtils : ANetUtils
{
    private UdpClient _udp;
    private UdpClient _udpClient;
    private ProgProperty _property;

    private bool _isPortAssign;
    
    public override Task Setup(ProgProperty property)
    {
        _udp = new UdpClient();
        _udpClient = new UdpClient();
        
        _property = property;
        _isPortAssign = false;
        return Task.CompletedTask;
    }
    public override async Task Send(byte[] msg)
    {
        Debug.WriteLine("UDP Sending...");
        IPAddress serverIp = ResolveDomain(_property.Url);
        _udp.Send(msg, msg.Length, serverIp.ToString(), _property.Port);
        _isPortAssign = true;
        Debug.WriteLine("UDP Send!");
    }

    public void SetListenPort()
    {
        _udpClient.Dispose();
        _udpClient = new UdpClient(_property.Port);
    }

    //Todo: cancelation token missing
    public override async Task<byte[]?> Receive(CancellationToken token)
    {
        //We need to find out which port was dynamicly allocated
        if(_isPortAssign)
        {
            Debug.WriteLine("UDP Receiving (waiting for data)...");
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _property.Port));
            UdpReceiveResult result = await _udpClient.ReceiveAsync(token);
            Debug.WriteLine($"UDP Receive: {Encoding.UTF8.GetString(result.Buffer)}");
            return result.Buffer;
        }
        return null;
    }

    public override void Dispose()
    {
        _udpClient.Dispose();
        _udp.Dispose();     // lepšia forma (z .NET Core vyššie)
    }
}