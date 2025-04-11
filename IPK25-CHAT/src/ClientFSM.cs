using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class ClientFsm
{
    private FsmStates _state;
    private NetworkUtils _networkUtils;
    private ProgProperty _progProperty;
    
    private UserProperty _userProperty;
    
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
                    await ProcessCommand(input);
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
                    await RunFsm(true, msg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    cts.Cancel();
                }
            }
        }, cts.Token);

        //Posunie sa ked skoncia vsetky tasky
        await Task.WhenAll(readFromStdinTask, readFromServerTask);
        
        Debug.WriteLine("ENDING Client");
        _networkUtils.Disconnect();
    }
    
    //Todo: Zmenit ukoncenie funkcie aby ukoncilo program nie cez throw exception
    private async Task RunFsm(bool forSend, string msg)
    {
        Debug.WriteLine($"IN STATE: {_state}");
        switch (_state)
        {
            case FsmStates.Start:
                if(forSend)
                {
                    await _networkUtils.Send(msg);
                    _state = FsmStates.Auth;
                }
                else
                    _state = FsmStates.End;
                break;
            case FsmStates.Auth:
                if(forSend)
                    await _networkUtils.Send(msg);
                else
                {
                    if (msg.StartsWith("REPLY NOT"))
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Auth));
                    else if(msg.StartsWith("REPLY OK"))
                        _state = FsmStates.Open;
                    else if(msg.StartsWith("ERR FROM") || msg.StartsWith("BYE FROM"))
                        _state = FsmStates.End;
                    else if (msg.StartsWith("MSG FROM"))
                    {
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Err));
                        _state = FsmStates.End;
                    }
                    else
                    {
                        await _networkUtils.Send(Output.Builder(_userProperty, MessageTypes.Bye));
                        _state = FsmStates.End;
                    }
                }
                
                break;
            case FsmStates.Open:
                _state = FsmStates.Join;
                break;
            case FsmStates.Join:
                _state = FsmStates.End;
                break;
            case FsmStates.End:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task ProcessCommand(string input)
    {
        MessageTypes? type;
        if ((type = InputMsgType(input)) != null)
        {
            var msg = Output.Builder(_userProperty, type);
            await RunFsm(true, msg);
        }
    }

    private MessageTypes? InputMsgType(string input)
    {
        Input.GrammarCheck(input);
        
        if (input.StartsWith("/rename"))
        {
            _userProperty.DisplayName = input.Split(" ")[1];
            return null;
        }

        if (input.StartsWith("/auth"))
        {
            _userProperty.Username = input.Split(" ")[1];
            _userProperty.DisplayName = input.Split(" ")[2];
            _userProperty.Secret = input.Split(" ")[3];
            return MessageTypes.Auth;
        }

        if (input.StartsWith("/join"))
        {
            _userProperty.ChanelId = input.Split(" ")[1];
            return MessageTypes.Join;
        }
        return MessageTypes.Msg;
    }
}