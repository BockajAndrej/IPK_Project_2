using IPK25_CHAT.Network;
using IPK25_CHAT.structs;
using Timer = System.Timers.Timer;

namespace IPK25_CHAT.FSM;

public abstract class AFsm
{
    protected FsmStates State;
    
    protected ProgProperty ProgProperty;
    protected UserProperty UserProperty;
    
    protected MessageTypes? LastOutputMsgType;
    protected MessageTypes? LastInputMsgType;

    protected ANetUtils NetworkUtils;

    protected volatile bool IsMsgSent;
    private Timer? _timer;

    protected AFsm(ProgProperty property)
    {
        State = FsmStates.Start;
        ProgProperty = property;
        
        if (property.IsTcp == true)
            NetworkUtils = new TcpUtils();
        else
            NetworkUtils = new UdpUtils();
    }
    
    private void ModifyUserProperty(string input)
    {
        if(Input.IsCommand(input))
        {
            if (input == "/help")
                return;
            
            if (input.Split(" ")[0] == "/rename")
            {
                UserProperty.DisplayName = input.Split(" ")[1];
            }

            if (input.Split(" ")[0] == "/auth")
            {
                UserProperty.Username = input.Split(" ")[1];
                UserProperty.Secret = input.Split(" ")[2];
                UserProperty.DisplayName = input.Split(" ")[3];
            }

            if (input.Split(" ")[0] == "/join")
            {
                UserProperty.ChanelId = input.Split(" ")[1];
            }
        }
        UserProperty.MessageContent = input;
    }
    private async Task ClientTask(string input)
    {
        LastInputMsgType = Input.MsgType(input);
        if (LastInputMsgType == MessageTypes.Rename)
            ModifyUserProperty(input);
        else if (LastInputMsgType == MessageTypes.Help)
            Input.PrintUsage();
        else
            await RunFsm(input);
    }
    private void WriteError(string input)
    {
        Console.WriteLine($"ERROR: {input}");
    }
    
    protected abstract void CleanUp();
    protected abstract Task NetworkSetup();
    protected abstract Task ServerTasks(CancellationToken token);
    protected abstract Task SnedMessage(MessageTypes? messageType);
    
    public async Task RunClient()
    {
        await NetworkSetup();
        
        CancellationTokenSource cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += (sender, e) =>
        {
            Debug.WriteLine("Zachytený Ctrl+C, spúšťam cleanup...");
            e.Cancel = true;
            cts.Cancel();
            Console.In.Dispose(); // should cancel ReadLine
        };
        
        _timer = new Timer(5000);
        _timer.Elapsed += (s, e) =>
        {
            cts.Cancel();
            _timer.Stop(); // stop repeated cancellation
        };
        
        //Receive stdin
        var readFromStdinTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                
                try
                {
                    string? input = Console.ReadLine();
                    //C-d ending
                    if (input != null) await ClientTask(input);
                    else throw new NullReferenceException();
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
                   await ServerTasks(cts.Token);
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
        
        CleanUp();
    }
    
    //FSM
    protected async Task RunFsm(string? input)
    {
        Debug.WriteLine($"IN STATE: {State}");
        switch (State)
        {
            case FsmStates.Start:
                await StartState(input);
                break;
            case FsmStates.Auth:
                await AuthState(input);
                break;
            case FsmStates.Open:
                await OpenState(input);
                break;
            case FsmStates.Join:
                await JoinState(input);
                break;
            case FsmStates.End:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private async Task StartState(string? input)
    {
        if(input != null)
        {
            if(LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Auth)
            {
                State = FsmStates.Auth;
                _timer?.Start();
            }
            else if (LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await SnedMessage(LastInputMsgType);
            return;
        }
        switch (LastOutputMsgType)
        {
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task AuthState(string? input)
    {
        if(input != null)
        {
            if(LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Auth)
            {
                State = FsmStates.Auth;
                _timer?.Stop();
                _timer?.Start();
            }
            else if (LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await SnedMessage(LastInputMsgType);
            return;
        }
        switch (LastOutputMsgType)
        {
            case MessageTypes.ReplyNok:
                _timer?.Stop();
                return;
            case MessageTypes.ReplyOk:
                State = FsmStates.Open;
                _timer?.Stop();
                return;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.Msg:
                await SnedMessage(MessageTypes.Err);
                throw new Exception();
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    private async Task OpenState(string? input)
    {
        if(input != null)
        {
            if(LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Join)
            {
                State = FsmStates.Join;
                _timer?.Start();
            }
            else if (LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else if (LastInputMsgType != null && LastInputMsgType.Value != MessageTypes.Msg)
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await SnedMessage(LastInputMsgType);
            return;
        }
        switch (LastOutputMsgType)
        {
            case MessageTypes.Msg:
                return;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.ReplyNok:
            case MessageTypes.ReplyOk:
                await SnedMessage(MessageTypes.Err);
                throw new Exception();
        }
        throw new ArgumentOutOfRangeException();
    }

    private Task JoinState(string? input)
    {
        if(input != null)
        {
            if (LastInputMsgType != null && LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            WriteError(input);
            return Task.CompletedTask;
        }
        switch (LastOutputMsgType)
        {
            case MessageTypes.Msg:
                return Task.CompletedTask;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.ReplyNok:
            case MessageTypes.ReplyOk:
                State = FsmStates.Open;
                _timer?.Stop();
                return Task.CompletedTask;
        }
        throw new ArgumentOutOfRangeException();
    }
}