using System.Text;
using IPK25_CHAT.Encryption.Interfaces;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Encryption;

public class UdpDecoder : IDecoder<byte[]>
{
    private MessageTypes? DecodeServer_MsgType(byte[] data)
    {
        switch (data[0])
        {
            case 0x00:
                return MessageTypes.Confirm;
            case 0x01:
                if(data[3] == 0x00)
                    return MessageTypes.ReplyNok;
                return MessageTypes.ReplyOk;
            case 0x02:
                return MessageTypes.Auth;
            case 0x03:
                return MessageTypes.Join;
            case 0x04:
                return MessageTypes.Msg;
            case 0xFE:
                return MessageTypes.Err;
            case 0xFF:
                return MessageTypes.Bye;
            case 0xFD:
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
    
    //Return type null when receive malformed msg
    public MessageTypes? ProcessMsg(byte[] data)
    {
        Queue<int> lengths = new Queue<int>();
        
        int currentIndex = 3;
        
        MessageTypes? msgType = DecodeServer_MsgType(data);
        if(msgType == null)
        {
            Console.WriteLine($"ERROR: {Encoding.UTF8.GetString(data)}");
            return msgType;
        }
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
                Console.WriteLine($"Action Success: {Encoding.UTF8.GetString(word)}");
                break;
            case MessageTypes.ReplyNok:
                len = lengths.Dequeue();
                currentIndex = 6;
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                Console.WriteLine($"Action Failure: {Encoding.UTF8.GetString(word)}");
                break;
            case MessageTypes.Msg:
                len = lengths.Dequeue();
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                
                currentIndex += len + 1;
                len = lengths.Dequeue();
                word2 = new byte[len];
                Array.Copy(data, currentIndex, word2, 0, len);
                
                Console.WriteLine($"{Encoding.UTF8.GetString(word)}: {Encoding.UTF8.GetString(word2)}");
                break;
            case MessageTypes.Err:
                len = lengths.Dequeue();
                word = new byte[len];
                Array.Copy(data, currentIndex, word, 0, len);
                
                currentIndex += len + 1;
                len = lengths.Dequeue();
                word2 = new byte[len];
                Array.Copy(data, currentIndex, word2, 0, len);
                
                Console.WriteLine($"ERROR FROM {Encoding.UTF8.GetString(word)}: {Encoding.UTF8.GetString(word2)}");
                break;
            default:
                throw new Exception("Income msg processing error");
        }
            
        //Defines according to last message
        return msgType;
    }
}