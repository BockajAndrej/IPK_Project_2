using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class ClientFsm
{
    private FsmStates _state;
    private NetworkUtils _networkUtils;
    
    private ProgProperty _progProperty;
    private UserProperty _userProperty;
    
    private MessageTypes? _lastOutputMsgType;
    private MessageTypes? _lastInputMsgType;
    
    public ClientFsm(ProgProperty property)
    {
        _state = FsmStates.Start;
        _networkUtils = new NetworkUtils();
        
        _progProperty = property;
    }

    public async Task RunClient()
    {
        await _networkUtils.Connect(_progProperty);
        
        using CancellationTokenSource cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += (sender, e) =>
        {
            Debug.WriteLine("Zachytený Ctrl+C, spúšťam cleanup...");
            e.Cancel = true;
            cts.Cancel();
            Console.In.Dispose(); // should cancel ReadLine
        };
        
        //Receive stdin
        var readFromStdinTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                string? input = Console.ReadLine();
                try
                {
                    if (input == null)
                        throw new NullReferenceException();
                    _lastInputMsgType = Input.SendMsgType(input);
                    if (_lastInputMsgType == null)
                        ModifyUserProperty(input);
                    else
                        await RunFsm(true, input);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    cts.Cancel();
                }
            }
        }, cts.Token);

        //Receive server
        var readFromServerTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    string msg = await _networkUtils.Receive(cts.Token);
                    _lastOutputMsgType = Input.IncomeMsgProcess(msg);
                    if (_lastOutputMsgType != null)
                        await RunFsm(false, msg);
                    else
                    {
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Err));
                        throw new NullReferenceException();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    cts.Cancel();
                }
            }
        }, cts.Token);
        
        //Readline stay active and will be terminated when program will be closed
        await Task.WhenAny(readFromStdinTask, readFromServerTask);

        Debug.WriteLine("ENDING Client");
        _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Bye));
        _networkUtils.Disconnect();
    }
    
    private async Task RunFsm(bool forSend, string input)
    {
        Debug.WriteLine($"IN STATE: {_state}");
        switch (_state)
        {
            case FsmStates.Start:
                if(forSend)
                {
                    if(_lastInputMsgType.Value == MessageTypes.Auth)
                        _state = FsmStates.Auth;
                    else if (_lastInputMsgType.Value == MessageTypes.Bye)
                        throw new Exception();
                    else
                    {
                        WriteError(input);
                        break;
                    }
                    ModifyUserProperty(input);
                    await _networkUtils.Send(Output.Builder(_userProperty, _lastInputMsgType));
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        throw new Exception();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            case FsmStates.Auth:
                if(forSend)
                {
                    if(_lastInputMsgType.Value == MessageTypes.Auth)
                        _state = FsmStates.Auth;
                    else if (_lastInputMsgType.Value == MessageTypes.Bye)
                        throw new Exception();
                    else
                    {
                        WriteError(input);
                        break;
                    }
                    ModifyUserProperty(input);
                    await _networkUtils.Send(Output.Builder(_userProperty, _lastInputMsgType));
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.ReplyNok:
                        break;
                    case MessageTypes.ReplyOk:
                        _state = FsmStates.Open;
                        break;
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        throw new Exception();
                        break;
                    case MessageTypes.Msg:
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Err));
                        throw new Exception();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            case FsmStates.Open:
                if(forSend)
                {
                    if(_lastInputMsgType.Value == MessageTypes.Join)
                        _state = FsmStates.Join;
                    else if (_lastInputMsgType.Value == MessageTypes.Bye)
                        throw new Exception();
                    else if (_lastInputMsgType.Value != MessageTypes.Msg)
                    {
                        WriteError(input);
                        break;
                    }
                    ModifyUserProperty(input);
                    await _networkUtils.Send(Output.Builder(_userProperty, _lastInputMsgType));
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.Msg:
                        break;
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        throw new Exception();
                        break;
                    case MessageTypes.ReplyNok:
                    case MessageTypes.ReplyOk:
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Err));
                        throw new Exception();
                        break;
                }
                break;
            case FsmStates.Join:
                if(forSend)
                {
                    if (_lastInputMsgType.Value == MessageTypes.Bye)
                        throw new Exception();
                    WriteError(input);
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.Msg:
                        break;
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        throw new Exception();
                    case MessageTypes.ReplyNok:
                    case MessageTypes.ReplyOk:
                        _state = FsmStates.Open;
                        break;
                }
                break;
            case FsmStates.End:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WriteError(string input)
    {
        Console.WriteLine($"ERROR: {input}");
    }

    private void ModifyUserProperty(string input)
    {
        if(Input.GrammarCheck(input))
        {
            if (input.Split(" ")[0] == "/rename")
            {
                _userProperty.DisplayName = input.Split(" ")[1];
            }

            if (input.Split(" ")[0] == "/auth")
            {
                _userProperty.Username = input.Split(" ")[1];
                _userProperty.Secret = input.Split(" ")[2];
                _userProperty.DisplayName = input.Split(" ")[3];
            }

            if (input.Split(" ")[0] == "/join")
            {
                _userProperty.ChanelId = input.Split(" ")[1];
            }
        }
        _userProperty.MessageContent = input;
    }
}