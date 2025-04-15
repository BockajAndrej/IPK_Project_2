using System.Text;
using IPK25_CHAT.Encryption;
using IPK25_CHAT.Encryption.Interfaces;
using IPK25_CHAT.Network;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.FSM;

public abstract class AFsm<T>
{
    protected FsmStates State;
    
    protected ProgProperty ProgProperty;
    protected UserProperty UserProperty;
    
    protected MessageTypes? LastOutputMsgType;
    protected MessageTypes? LastInputMsgType;

    protected ANetUtils NetworkUtils;
    
    private IDecoder<T> _decoder;

    protected volatile bool IsMsgSent;

    protected AFsm(ProgProperty property)
    {
        State = FsmStates.Start;
        ProgProperty = property;
        
        if (property.IsTcp == true)
        {
            NetworkUtils = new TcpUtils();
            _decoder = (IDecoder<T>)new TcpDecoder();
        }
        else
        {
            NetworkUtils = new UdpUtils();
            _decoder = (IDecoder<T>)new UdpDecoder();
        }
    }
    
    protected abstract void CleanUp();
    protected abstract Task NetworkSetup();

    protected abstract Task SnedMessage(UserProperty userProperty, MessageTypes? messageType);
    
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
        
        //Receive stdin
        var readFromStdinTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                string? input = Console.ReadLine();
                try
                {
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
    
    protected  async Task ClientTask(string input)
    {
        LastInputMsgType = Input.MsgType(input);
        if (LastInputMsgType == null)
            ModifyUserProperty(input);
        else
            await RunFsm(input);
    }

    protected  async Task ServerTasks(CancellationToken token)
    {
        byte[]? msg = await NetworkUtils.Receive(token);
        if(msg != null)
        {
            T typeT;
            if (typeof(T) == typeof(string))
            {
                string decoded = Encoding.UTF8.GetString(msg);
                typeT = (T)(object)decoded.TrimEnd('\0');
            }
            else
                typeT = (T)(object)msg;

            LastOutputMsgType = _decoder.ProcessMsg(typeT);
            if (LastOutputMsgType != null)
                if (IsMsgSent && LastInputMsgType == MessageTypes.Confirm)
                    IsMsgSent = false;
                else
                    await RunFsm(null);
            else
            {
                await SnedMessage(UserProperty, MessageTypes.Err);
                throw new NullReferenceException();
            }
        }
        else
            Thread.Sleep(50);
    }
    
    protected void ModifyUserProperty(string input)
    {
        if(Input.IsCommand(input))
        {
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
    
    
    //Methods for FSM individual states
    protected async Task StartState(string? input)
    {
        if(input != null)
        {
            if(LastInputMsgType.Value == MessageTypes.Auth)
                State = FsmStates.Auth;
            else if (LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await SnedMessage(UserProperty, LastInputMsgType);
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

    protected async Task AuthState(string? input)
    {
        if(input != null)
        {
            if(LastInputMsgType.Value == MessageTypes.Auth)
                State = FsmStates.Auth;
            else if (LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await SnedMessage(UserProperty, LastInputMsgType);
            return;
        }
        switch (LastOutputMsgType)
        {
            case MessageTypes.ReplyNok:
                return;
            case MessageTypes.ReplyOk:
                State = FsmStates.Open;
                return;
            case MessageTypes.Err:
            case MessageTypes.Bye:
                throw new Exception();
            case MessageTypes.Msg:
                await SnedMessage(UserProperty, MessageTypes.Err);
                throw new Exception();
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    protected async Task OpenState(string? input)
    {
        if(input != null)
        {
            if(LastInputMsgType.Value == MessageTypes.Join)
                State = FsmStates.Join;
            else if (LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            else if (LastInputMsgType.Value != MessageTypes.Msg)
            {
                WriteError(input);
                return;
            }
            ModifyUserProperty(input);
            await SnedMessage(UserProperty, LastInputMsgType);
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
                await SnedMessage(UserProperty, MessageTypes.Err);
                throw new Exception();
        }
        throw new ArgumentOutOfRangeException();
    }

    protected async Task JoinState(string? input)
    {
        if(input != null)
        {
            if (LastInputMsgType.Value == MessageTypes.Bye)
                throw new Exception();
            WriteError(input);
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
                State = FsmStates.Open;
                return;
        }
        throw new ArgumentOutOfRangeException();
    }

    
    private void WriteError(string input)
    {
        Console.WriteLine($"ERROR: {input}");
    }
}