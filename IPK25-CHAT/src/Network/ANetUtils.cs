using System.Net;
using System.Net.Sockets;

namespace IPK25_CHAT;

public abstract class ANetUtils
{
    public abstract Task Send(string msg);
    public abstract Task<string> Receive(CancellationToken token);
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