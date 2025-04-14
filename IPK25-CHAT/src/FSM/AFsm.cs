using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public abstract class AFsm
{
    protected FsmStates _state;
    
    protected ProgProperty _progProperty;
    protected UserProperty _userProperty;
    
    protected MessageTypes? _lastOutputMsgType;
    protected MessageTypes? _lastInputMsgType;

    protected AFsm(ProgProperty property)
    {
        _state = FsmStates.Start;
        _progProperty = property;
    }
    
    protected abstract void CleanUp();
    protected abstract Task NetworkSetup();
    protected abstract Task ClientTask(string input);
    protected abstract Task ServerTasks(CancellationTokenSource cts);
    
    protected abstract Task startState(string? input);
    protected abstract Task authState(string? input);
    protected abstract Task openState(string? input);
    protected abstract Task joinState(string? input);
    
    public async Task RunClient()
    {
        await NetworkSetup();
        
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
                   await ServerTasks(cts);
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
    
    protected async Task RunFsm(string? input)
    {
        Debug.WriteLine($"IN STATE: {_state}");
        switch (_state)
        {
            case FsmStates.Start:
                await startState(input);
                break;
            case FsmStates.Auth:
                await authState(input);
                break;
            case FsmStates.Open:
                await openState(input);
                break;
            case FsmStates.Join:
                await joinState(input);
                break;
            case FsmStates.End:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    protected void ModifyUserProperty(string input)
    {
        if(Input.isCommand(input))
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