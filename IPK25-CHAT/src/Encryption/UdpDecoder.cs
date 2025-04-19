using System.Text;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Encryption;

public class UdpDecoder
{
    private int msgId = 0;
    
    int CountZeroBytes(byte[] data, int startIndex)
    {
        int count = 0;
        for (int i = startIndex; i < data.Length; i++)
        {
            if (data[i] == 0)
            {
                count++;
            }
        }
        return count;
    }
    
    public MessageTypes? DecodeServer_MsgType(byte[] data)
    {
        switch (data[0])
        {
            case 0x00:
                if (data.Length != 3)
                    return null;
                return MessageTypes.Confirm;
            case 0x01:
                if (data.Length < 8)
                    return null;
                if(data[3] == 0x00)
                    return MessageTypes.ReplyNok;
                return MessageTypes.ReplyOk;
            case 0x02:
                if (data.Length < 9)
                    return null;
                return MessageTypes.Auth;
            case 0x03:
                if (data.Length < 7)
                    return null;
                return MessageTypes.Join;
            case 0x04:
                if (data.Length < 7 || CountZeroBytes(data, 3) == 1)
                    return null;
                return MessageTypes.Msg;
            case 0xFE:
                if (data.Length < 7)
                    return null;
                return MessageTypes.Err;
            case 0xFF:
                if (data.Length < 5)
                    return null;
                return MessageTypes.Bye;
            case 0xFD:
                if (data.Length != 3)
                    return null;
                return MessageTypes.Ping;
        }
        return null;
    }

    private int NumberOfBytesToRead(byte[] data, int startIndex)
    {
        int cnt = 0;
        if(startIndex < data.Length)
        {
            while (data[startIndex + cnt] != 0x00)
                cnt++;
        }
        return cnt;
    }
    
    public int getLastMsgId() => msgId;
    
    //Return type null when receive malformed msg
    public string ProcessMsg(byte[] data, MessageTypes? msgType)
    {
        if (data.Length < 3 || msgType == null)
        {
            return $"ERROR: {Encoding.UTF8.GetString(data)}";
        }
        
        Queue<int> lengths = new Queue<int>();
        
        int currentIndex = 3;
        
        byte[] bigEndianBytes  = new byte[2];
        Array.Copy(data, 1, bigEndianBytes , 0, 2);
        Array.Reverse(bigEndianBytes);
        msgId = BitConverter.ToInt16(bigEndianBytes , 0);
        
        if (msgType.Value == MessageTypes.ReplyOk || msgType.Value == MessageTypes.ReplyNok)
            currentIndex = 6;

        //0. byte = type, 1.,2. = id_msg
        int length = NumberOfBytesToRead(data, currentIndex);

        while (length > 0)
        {
            lengths.Enqueue(length);
            currentIndex += length;

            // read next length at the updated position
            length = NumberOfBytesToRead(data, currentIndex + 1);
        }

        int len;
        byte[] word;
        currentIndex = 3;
        byte[] word2;
        switch (msgType)
        {
            case MessageTypes.Confirm:
            case MessageTypes.Bye:
            case MessageTypes.Ping:
                break;
            case MessageTypes.ReplyOk:
                len = lengths.Dequeue();
                currentIndex = 6;
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                return $"Action Success: {Encoding.UTF8.GetString(word)}";
            case MessageTypes.ReplyNok:
                len = lengths.Dequeue();
                currentIndex = 6;
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                return $"Action Failure: {Encoding.UTF8.GetString(word)}";
            case MessageTypes.Msg:
                len = lengths.Dequeue();
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                
                currentIndex += len + 1;
                len = lengths.Dequeue();
                word2 = new byte[len];
                Array.Copy(data, currentIndex, word2, 0, len);
                
                return $"{Encoding.UTF8.GetString(word)}: {Encoding.UTF8.GetString(word2)}";
            case MessageTypes.Err:
                len = lengths.Dequeue();
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                
                currentIndex += len + 1;
                len = lengths.Dequeue();
                word2 = new byte[len];
                Array.Copy(data, currentIndex, word2, 0, len);
                
                return $"ERROR FROM {Encoding.UTF8.GetString(word)}: {Encoding.UTF8.GetString(word2)}";
            default:
                return $"ERROR: {Encoding.UTF8.GetString(data)}";
        }
            
        //Defines according to last message
        return "";
    }
}