namespace IPK25_CHAT;

public class UdpUtils : ANetUtils
{
    public override Task Send(string msg)
    {
        throw new NotImplementedException();
    }

    public override Task<string> Receive(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}