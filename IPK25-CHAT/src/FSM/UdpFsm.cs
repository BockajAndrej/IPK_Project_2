using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public class UdpFsm : AFsm
{
    private UdpUtils _networkUtils;
    
    public UdpFsm(ProgProperty property) : base(property)
    {
        _networkUtils = new UdpUtils();
    }

    protected override void CleanUp()
    {
        _networkUtils.Dispose();
    }

    protected override async Task NetworkSetup()
    {
        _networkUtils.Setup();
    }

    protected override async Task ClientTask(string input)
    {
        _lastInputMsgType = Input.MsgType(input);
        ModifyUserProperty(input);
        await RunFsm(input);
    }

    protected override async Task ServerTasks(CancellationTokenSource cancellationTokenSource)
    {
        Console.ReadKey();
    }

    protected override Task startState(string? input)
    {
        _networkUtils.Send(UdpEncoder.Builder(_userProperty, _lastInputMsgType), _progProperty);
        throw new NotImplementedException();
    }

    protected override Task authState(string? input)
    {
        throw new NotImplementedException();
    }

    protected override Task openState(string? input)
    {
        throw new NotImplementedException();
    }

    protected override Task joinState(string? input)
    {
        throw new NotImplementedException();
    }
}