using System.Text;
using IPK25_CHAT.Encryption;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public class TcpFsm : AFsm<string>
{
    private TcpDecoder _decoder;
    private TcpEncoder _encoder;
    
    public TcpFsm(ProgProperty property) : base(property)
    {
        _encoder = new TcpEncoder();
        _decoder = new TcpDecoder();
    }

    protected override void CleanUp()
    {
        NetworkUtils.Send(Encoding.UTF8.GetBytes(_encoder.Builder(UserProperty, MessageTypes.Bye)));
        NetworkUtils.Dispose();
    }

    protected override async Task NetworkSetup()
    {
        await NetworkUtils.Setup(ProgProperty);
    }

    protected override async Task SnedMessage(MessageTypes? messageType)
    {
        await NetworkUtils.Send(Encoding.UTF8.GetBytes(_encoder.Builder(UserProperty, messageType)));
    }
    
    protected override async Task ServerTasks(CancellationToken token)
    {
        byte[]? msg = await NetworkUtils.Receive(token);
        string decoded = Encoding.UTF8.GetString(msg).TrimEnd('\0');
        try
        {
            LastOutputMsgType = _decoder.ProcessMsg(decoded);
            if (LastOutputMsgType != null)
                await RunFsm(null);
        }
        catch (Exception ex)
        {
            await SnedMessage(MessageTypes.Err);
            throw new NullReferenceException();
        }
    }
}