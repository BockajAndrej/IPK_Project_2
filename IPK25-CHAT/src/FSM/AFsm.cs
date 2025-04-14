using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public abstract class AFsm
{
    protected FsmStates _state;
    
    protected ProgProperty _progProperty;
    protected UserProperty _userProperty;

    protected AFsm(ProgProperty property)
    {
        _state = FsmStates.Start;
        _progProperty = property;
    }
    
    protected abstract void CleanUp();
    protected abstract Task NetworkSetup();
    protected abstract Task ClientTask(string input);
    protected abstract Task ServerTasks(CancellationTokenSource cts);
    
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
    
}