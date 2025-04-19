using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Network;

public class UdpUtils : ANetUtils
{
    private UdpClient _udp = null!;
    private ProgProperty _property;
    
    private volatile bool _isMsgSent;
    
    public override Task Setup(ProgProperty property)
    {
        _udp = new UdpClient();
        
        _property = property;
        return Task.CompletedTask;
    }
    public override Task Send(byte[] msg)
    {
        Debug.WriteLine("UDP Sending...");
        IPAddress serverIp = ResolveDomain(_property.Url ?? throw new InvalidOperationException());
        _udp.Send(msg, msg.Length, serverIp.ToString(), _property.Port);
        _isMsgSent = true;
        Debug.WriteLine("UDP Send!");
        return Task.CompletedTask;
    }

    public override async Task<byte[]?> Receive(CancellationToken token)
    {
        //We need to find out which port was dynamicly allocated
        Debug.WriteLine("UDP Receiving (waiting for data)...");
        //_udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _property.Port));
        SpinWait.SpinUntil(() => _isMsgSent);
        UdpReceiveResult result = await _udp.ReceiveAsync(token);
        Debug.WriteLine($"UDP Receive: {Encoding.UTF8.GetString(result.Buffer)} from port: {result.RemoteEndPoint.Port}");
        _property.Port = result.RemoteEndPoint.Port;
        return result.Buffer;
            
    }

    public override void Dispose()
    {
        _udp.Dispose();     // lepšia forma (z .NET Core vyššie)
    }
}