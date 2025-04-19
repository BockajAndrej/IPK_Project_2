using System.Text;
using IPK25_CHAT.Encryption;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public class UdpFsm : AFsm<byte[]>
{
    private UdpDecoder _decoder;
    private UdpEncoder _encoder;
    private HashSet<int> zadaneCisla;
    
    
    public UdpFsm(ProgProperty property) : base(property)
    {
        _encoder = new UdpEncoder();
        _decoder = new UdpDecoder();
        zadaneCisla = new HashSet<int>();
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
        }
        
    }
    
    
    protected override async Task ServerTasks(CancellationToken token)
    {
        byte[]? msg = await NetworkUtils.Receive(token);

        LastOutputMsgType = _decoder.DecodeServer_MsgType(msg);
        var msgOutput = _decoder.ProcessMsg(msg, LastOutputMsgType);
        
        if (LastOutputMsgType != null)
        {
            int msgId = _decoder.getLastMsgId();

            if(LastOutputMsgType != MessageTypes.Confirm && LastOutputMsgType != MessageTypes.Bye && LastOutputMsgType != MessageTypes.Ping && !zadaneCisla.Contains(msgId))
            {
                Console.WriteLine(msgOutput);
                zadaneCisla.Add(msgId);
            }
            
            //Ignoring increment when waiting for confirm or delay confirm was received
            bool waitingForConfirm = msgId < UserProperty.MessageId && LastOutputMsgType == MessageTypes.Confirm;
            if (!(waitingForConfirm || IsMsgSent))
                UserProperty.MessageId = msgId;
        
        
            //Ignoring another messages until confirm
            if (IsMsgSent)
            {
                if(LastOutputMsgType == MessageTypes.Confirm)
                    IsMsgSent = false;
            }
            else
            {
                await SnedMessage(MessageTypes.Confirm);
                UserProperty.MessageId++;
                await RunFsm(null);
            }
        }
        else
        {
            Console.WriteLine(msgOutput);
            await SnedMessage(MessageTypes.Confirm);
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
        if(IsMsgSent)
            throw new Exception("Server has timed out");
    }
    
}