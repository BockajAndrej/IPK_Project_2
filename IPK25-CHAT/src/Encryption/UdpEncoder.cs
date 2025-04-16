using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Encryption;

public class UdpEncoder
{
    public byte[] Builder(UserProperty userProperty, MessageTypes? messageType)
    {
        byte[] data;
        int i = 3;
        byte[]? displayName;
        byte[]? msgContent;
        switch (messageType)
        {
            case MessageTypes.Confirm:
                data = new byte[3];
                data[0] = 0x00;
                break;
            case MessageTypes.Auth:
                var userName = Encoding.UTF8.GetBytes(userProperty.Username);
                displayName = Encoding.UTF8.GetBytes(userProperty.DisplayName);
                var secret = Encoding.UTF8.GetBytes(userProperty.Secret);
                data = new byte[3 + userName.Length + 1 + displayName.Length + 1 + secret.Length + 1];
                data[0] = 0x02;
                foreach (var b in userName)
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in displayName)
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in secret)
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Join:
                var chanelId = Encoding.UTF8.GetBytes(userProperty.ChanelId);
                displayName = Encoding.UTF8.GetBytes(userProperty.DisplayName);
                data = new byte[3 + chanelId.Length + 1 + displayName.Length + 1];
                
                data[0] = 0x03;
                foreach (var b in chanelId)
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in displayName)
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Msg:
                displayName = Encoding.UTF8.GetBytes(userProperty.DisplayName);
                msgContent = Encoding.UTF8.GetBytes(userProperty.MessageContent);
                data = new byte[3 + displayName.Length + 1 + msgContent.Length + 2 + 1];
                
                data[0] = 0x04;
                foreach (var b in displayName)
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in msgContent)
                    data[i++] = b;
                //\r\n
                data[i++] = 0x0D;
                data[i++] = 0x0A;
                data[i++] = 0x00;
                break;
            case MessageTypes.Err:
                displayName = Encoding.UTF8.GetBytes(userProperty.DisplayName);
                msgContent = Encoding.UTF8.GetBytes(userProperty.MessageContent);
                data = new byte[3 + displayName.Length + 1 + msgContent.Length + 1];
                
                data[0] = 0xFE;
                foreach (var b in displayName)
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in msgContent)
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Bye:
                if (userProperty.DisplayName == null)
                    userProperty.DisplayName = "Undefined";
                displayName = Encoding.UTF8.GetBytes(userProperty.DisplayName);
                data = new byte[3 + displayName.Length + 1];
                
                data[0] = 0xFF;
                foreach (var b in displayName)
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Ping:
                data = new byte[3];
                data[0] = 0xFD;
                break;
            default:
                throw new ArgumentException("Invalid message type");
        }
        data[1] = (byte)(userProperty.MessageId >> 8);     // high byte
        data[2] = (byte)(userProperty.MessageId & 0xFF);   // low byte
        return data;
    }
}