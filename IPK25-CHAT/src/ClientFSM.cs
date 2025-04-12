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
                if (input == null)
                    continue;
                try
                {
                    _lastInputMsgType = Input.SendMsgType(input, ref _userProperty);
                    if (_lastInputMsgType != null)
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
    
    //Todo: Zmenit ukoncenie funkcie aby ukoncilo program nie cez throw exception
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
                        _state = FsmStates.End;
                    else
                    {
                        WriteError(input);
                        break;
                    }
                    
                    await _networkUtils.Send(Output.Builder(_userProperty, _lastInputMsgType));
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        _state = FsmStates.End;
                        break;
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
                        _state = FsmStates.End;
                    else
                    {
                        WriteError(input);
                        break;
                    }
                    
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
                        _state = FsmStates.End;
                        break;
                    case MessageTypes.Msg:
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Err));
                        _state = FsmStates.End;
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
                        _state = FsmStates.End;
                    else if (_lastInputMsgType.Value != MessageTypes.Msg)
                    {
                        WriteError(input);
                        break;
                    }
                    
                    await _networkUtils.Send(Output.Builder(_userProperty, _lastInputMsgType));
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.Msg:
                        break;
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        _state = FsmStates.End;
                        break;
                    case MessageTypes.ReplyNok:
                    case MessageTypes.ReplyOk:
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Err));
                        _state = FsmStates.End;
                        break;
                }
                break;
            case FsmStates.Join:
                if(forSend)
                {
                    if (_lastInputMsgType.Value == MessageTypes.Bye)
                        _state = FsmStates.End;
                    else
                    {
                        WriteError(input);
                        break;
                    }
                    
                    await _networkUtils.Send(Output.Builder(_userProperty, _lastInputMsgType));
                    break;
                }
                switch (_lastOutputMsgType)
                {
                    case MessageTypes.Msg:
                        break;
                    case MessageTypes.Err:
                    case MessageTypes.Bye:
                        _state = FsmStates.End;
                        break;
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
}