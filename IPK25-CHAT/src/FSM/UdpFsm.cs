using System.Text;
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

    protected override async void CleanUp()
    {
        await SnedMessage(MessageTypes.Bye);
        NetworkUtils.Dispose();
    }

    protected override Task NetworkSetup()
    {
        NetworkUtils.Setup(ProgProperty);
        return Task.CompletedTask;
    }

    protected override async Task SnedMessage(MessageTypes? messageType)
    {
        Debug.WriteLine($"SnedMessage with == {UserProperty.MessageId}");
        NetworkUtils.Send(_encoder.Builder(UserProperty, messageType));
        
        if (messageType != MessageTypes.Confirm)
        {
            IsMsgSent = true;
            await RetransmitTasks(UserProperty, messageType);
            UserProperty.MessageId++;
        }
        
    }
    
    
    protected override async Task ServerTasks(CancellationToken token)
    {
        byte[]? msg = await NetworkUtils.Receive(token);
        
        LastOutputMsgType = _decoder.ProcessMsg(msg);
        int msgId = _decoder.getLastMsgId();
        
        if(msgId < UserProperty.MessageId)
            UserProperty.MessageId = msgId;
        
        if (LastOutputMsgType != null)
        {
            if (IsMsgSent && (LastOutputMsgType == MessageTypes.Confirm))
                IsMsgSent = false;
            else
            {
                await SnedMessage(MessageTypes.Confirm);
                UserProperty.MessageId++;
                await RunFsm(null);
            }
        }
        else
        {
            await SnedMessage(MessageTypes.Err);
            throw new NullReferenceException();
        }
    }

    protected async Task RetransmitTasks(UserProperty userProperty, MessageTypes? messageType)
    {
        var lastLogTime = DateTime.UtcNow;
        int numberOfTransmits = 0;

        while (IsMsgSent && (numberOfTransmits < ProgProperty.NumberOfRetransmits))
        {
            if ((DateTime.UtcNow - lastLogTime).TotalMilliseconds >= ProgProperty.Timeout)
            {
                //Console.WriteLine($"V Posielani = {(DateTime.UtcNow - lastLogTime).TotalMilliseconds} > {ProgProperty.Timeout}");
                NetworkUtils.Send(_encoder.Builder(userProperty, messageType));
                lastLogTime = DateTime.UtcNow;
                numberOfTransmits++;
            }
            await Task.Delay(10);
        }
    }
    
}