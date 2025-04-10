namespace IPK25_CHAT;

public class ClientFsm
{
    private FsmStates _state;

    private enum FsmStates
    {
        Start, 
        Auth,
        Open,
        Join,
        End
    }

    public ClientFsm()
    {
        _state = FsmStates.Start;
    }

    public void RunClientFsm()
    {
        switch (_state)
        {
            case FsmStates.Start:
                break;
            case FsmStates.Auth:
                break;
            case FsmStates.Open:
                break;
            case FsmStates.Join:
                break;
            case FsmStates.End:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}