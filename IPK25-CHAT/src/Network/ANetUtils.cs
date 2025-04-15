using System.Net;
using System.Net.Sockets;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Network;

public abstract class ANetUtils
{
    public abstract void Dispose();
    public abstract Task Send(byte[] msg);
    public abstract Task<byte[]?> Receive(CancellationToken token);
    public abstract Task Setup(ProgProperty prop);
    
    public IPAddress ResolveDomain(string serverUrl)
    {
        IPAddress[] addresses = Dns.GetHostAddresses(serverUrl);
        foreach (IPAddress addr in addresses)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork) // IPv4 only
            {
                return addr;
            }
        }
        throw new Exception("Could not resolve domain to an IP address.");
    }
}