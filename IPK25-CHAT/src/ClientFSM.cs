using System.Threading.Channels;
using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class ClientFsm
{
    private FsmStates _state;
    private NetworkUtils _networkUtils;
    private ProgProperty _property;
    
    public ClientFsm(ProgProperty property)
    {
        _state = FsmStates.Start;
        _networkUtils = new NetworkUtils();
        _property = property;
    }

    public async Task RunClient()
    {
        await _networkUtils.Connect(_property);
        
        using CancellationTokenSource cts = new CancellationTokenSource();
        
        //Receive stdin
        var readFromStdinTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                string? input = Console.ReadLine();
                try
                {
                    Input.GrammarCheck(input);
                    string msg = Output.Build(input);
                    await RunFsm(true, msg);
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
        
        Debug.WriteLine("ENDING PICI");
        _networkUtils.Disconnect();
    }
    
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
                _state = FsmStates.Open;
                break;
            case FsmStates.Open:
                _state = FsmStates.Join;
                break;
            case FsmStates.Join:
                _state = FsmStates.End;
                break;
            case FsmStates.End:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private enum FsmStates
    {
        Start, 
        Auth,
        Open,
        Join,
        End
    }
}