using IPK25_CHAT.Encryption;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public class UdpFsm : AFsm<byte[]>
{
    private UdpEncoder _encoder;
    
    public UdpFsm(ProgProperty property) : base(property)
    {
        _encoder = new UdpEncoder();
    }

    protected override void CleanUp()
    {
        NetworkUtils.Send(_encoder.Builder(UserProperty, MessageTypes.Bye));
        NetworkUtils.Dispose();
    }

    protected override Task NetworkSetup()
    {
        NetworkUtils.Setup(ProgProperty);
        return Task.CompletedTask;
    }

    protected override Task SnedMessage(UserProperty userProperty, MessageTypes? messageType)
    {
        NetworkUtils.Send(_encoder.Builder(UserProperty, messageType));
        IsMsgSent = true;
        var lastLogTime = DateTime.UtcNow;
        int numberOfTransmits = 0;
        while (!IsMsgSent && (numberOfTransmits < ProgProperty.NumberOfRetransmits))
        {
            if ((DateTime.UtcNow - lastLogTime).TotalMicroseconds >= ProgProperty.Timeout)
            {
                NetworkUtils.Send(_encoder.Builder(UserProperty, messageType));
                lastLogTime = DateTime.UtcNow;
                numberOfTransmits++;
            }

            Thread.Sleep(50); // krátky spánok kvôli šetreniu CPU
        }
        return Task.CompletedTask;
    }
    
    
}