using IPK25_CHAT.structs;

namespace IPK25_CHAT.ioStream;

public static class Output
{
    public static string Builder(UserProperty userProperty, MessageTypes? messageType)
    {
        switch (messageType)
        {
            //User cases
            case MessageTypes.Auth:
                return $"AUTH {userProperty.Username} AS {userProperty.DisplayName} USING {userProperty.Secret}\r\n";
            case MessageTypes.Join:
                return $"JOIN {userProperty.ChanelId} AS {userProperty.DisplayName}\r\n";
            case MessageTypes.Msg:
                return $"MSG FROM {userProperty.DisplayName} IS {userProperty.MessageContent}\r\n";
            case MessageTypes.Bye:
                return $"BYE FROM {userProperty.DisplayName}\r\n";
            //Program cases
            case MessageTypes.Err:
                return $"ERR FROM {userProperty.DisplayName} IS {userProperty.MessageContent}\r\n";
            default:
                throw new ArgumentException("Invalid message type");
        }
        
    }
}