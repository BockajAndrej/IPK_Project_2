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

    protected override async Task SnedMessage(UserProperty userProperty, MessageTypes? messageType)
    {
        await NetworkUtils.Send(Encoding.UTF8.GetBytes(_encoder.Builder(UserProperty, messageType)));
    }
}