using System.Text;
using IPK25_CHAT.Encryption;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public class TcpFsm : AFsm<string>
{
    private TcpEncoder _encoder;
    public TcpFsm(ProgProperty property) : base(property)
    {
        _encoder = new TcpEncoder();
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
        LastOutputMsgType = _decoder.ProcessMsg(decoded);
        if (LastOutputMsgType != null)
            await RunFsm(null);
        else
        {
            await SnedMessage(MessageTypes.Err);
            throw new NullReferenceException();
        }
    }
}