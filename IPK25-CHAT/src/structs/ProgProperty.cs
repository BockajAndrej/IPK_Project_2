namespace IPK25_CHAT.structs;

public struct ProgProperty(bool? isTcp, string? url, int port, int timeout, int numberOfRetransmits)
{
    public bool? IsTcp = isTcp;
    public string? Url = url;
    public int Port = port;
    public int Timeout = timeout;
    public int NumberOfRetransmits = numberOfRetransmits;
}