using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public class UdpFsm : AFsm
{
    public UdpFsm(ProgProperty property) : base(property)
    {
    }

    protected override void CleanUp()
    {
        throw new NotImplementedException();
    }

    protected override Task NetworkSetup()
    {
        throw new NotImplementedException();
    }

    protected override Task ClientTask(string input)
    {
        throw new NotImplementedException();
    }

    protected override Task ServerTasks(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }
}