using IPK25_CHAT.structs;

namespace IPK25_CHAT.Encryption.Interfaces;

public interface IDecoder<T>
{
    public MessageTypes? ProcessMsg(T input);
    public int getLastMsgId();
}