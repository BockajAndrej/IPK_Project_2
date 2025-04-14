using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public class TcpFsm : AFsm
{
    private TcpUtils _networkUtils;

    public TcpFsm(ProgProperty property) : base(property)
    {
        _networkUtils = new TcpUtils();
    }

    protected override void CleanUp()
    {
        _networkUtils.Send(TcpEncoder.Builder(_userProperty, MessageTypes.Bye));
        _networkUtils.Dispose();
    }

    protected override async Task NetworkSetup()
    {
        await _networkUtils.Connect(_progProperty);
    }

    protected override async Task ClientTask(string input)
    {
        _lastInputMsgType = Input.MsgType(input);
        if (_lastInputMsgType == null)
            ModifyUserProperty(input);
        else
            await RunFsm(input);
    }

    protected override async Task ServerTasks(CancellationTokenSource cts)
    {
        string msg = await _networkUtils.Receive(cts.Token);
        _lastOutputMsgType = TcpDecoder.ProcessMsg(msg);
        if (_lastOutputMsgType != null)
            await RunFsm(null);
        else
        {
            await _networkUtils.Send(TcpEncoder.Builder(_userProperty, MessageTypes.Err));
            throw new NullReferenceException();
        }
    }

    protected override async Task startState(string? input)
    {
        if(input != null)
        {
            if(_lastInputMsgType.Value == MessageTypes.Auth)
                _state = FsmStates.Auth;
            else if (_lastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await _networkUtils.Send(TcpEncoder.Builder(_userProperty, _lastInputMsgType));
            return;
        }
        switch (_lastOutputMsgType)
        {
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override async Task authState(string? input)
    {
        if(input != null)
        {
            if(_lastInputMsgType.Value == MessageTypes.Auth)
                _state = FsmStates.Auth;
            else if (_lastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await _networkUtils.Send(TcpEncoder.Builder(_userProperty, _lastInputMsgType));
            return;
        }
        switch (_lastOutputMsgType)
        {
            case MessageTypes.ReplyNok:
                return;
            case MessageTypes.ReplyOk:
                _state = FsmStates.Open;
                return;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.Msg:
                await _networkUtils.Send(TcpEncoder.Builder(_userProperty, MessageTypes.Err));
                throw new Exception();
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    protected override async Task openState(string? input)
    {
        if(input != null)
        {
            if(_lastInputMsgType.Value == MessageTypes.Join)
                _state = FsmStates.Join;
            else if (_lastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else if (_lastInputMsgType.Value != MessageTypes.Msg)
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await _networkUtils.Send(TcpEncoder.Builder(_userProperty, _lastInputMsgType));
            return;
        }
        switch (_lastOutputMsgType)
        {
            case MessageTypes.Msg:
                return;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.ReplyNok:
            case MessageTypes.ReplyOk:
                await _networkUtils.Send(TcpEncoder.Builder(_userProperty, MessageTypes.Err));
                throw new Exception();
        }
        throw new ArgumentOutOfRangeException();
    }

    protected override async Task joinState(string? input)
    {
        if(input != null)
        {
            if (_lastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            WriteError(input);
            return;
        }
        switch (_lastOutputMsgType)
        {
            case MessageTypes.Msg:
                return;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.ReplyNok:
            case MessageTypes.ReplyOk:
                _state = FsmStates.Open;
                return;
        }
        throw new ArgumentOutOfRangeException();
    }

    
    private void WriteError(string input)
    {
        Console.WriteLine($"ERROR: {input}");
    }
}