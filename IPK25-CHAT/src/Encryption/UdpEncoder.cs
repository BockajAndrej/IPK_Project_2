using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.ioStream;

public static class UdpEncoder
{
    public static byte[] Builder(UserProperty userProperty, MessageTypes? messageType)
    {
        byte[] data;
        int i = 3;
        switch (messageType)
        {
            case MessageTypes.Confirm:
                data = new byte[3];
                data[0] = 0x00;
                break;
            case MessageTypes.Auth:
                var userName = Encoding.UTF8.GetBytes(userProperty.Username);
                var displayName = Encoding.UTF8.GetBytes(userProperty.DisplayName);
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
                data = new byte[3];
                data[0] = 0x03;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.ChanelId))
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.DisplayName))
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Msg:
                data = new byte[3];
                data[0] = 0x04;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.DisplayName))
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.MessageContent))
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Err:
                data = new byte[3];
                data[0] = 0xFE;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.DisplayName))
                    data[i++] = b;
                data[i++] = 0x00;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.MessageContent))
                    data[i++] = b;
                data[i++] = 0x00;
                break;
            case MessageTypes.Bye:
                data = new byte[3];
                data[0] = 0xFF;
                foreach (var b in Encoding.UTF8.GetBytes(userProperty.DisplayName))
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