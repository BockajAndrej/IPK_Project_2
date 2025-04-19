using System.Text.RegularExpressions;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Encryption;

public class TcpDecoder
{
    private string _savedInput = "";
    
    private MessageTypes? DecodeServer_MsgType(string input)
    {
        switch (input.Split(" ")[0])
        {
            case "ERR": 
                return MessageTypes.Err;
            case "REPLY": 
            {
                if(input.Split(" ")[1] == "OK")
                    return MessageTypes.ReplyOk;
                return MessageTypes.ReplyNok;
            }
            case "AUTH": 
                return MessageTypes.Auth;
            case "JOIN": 
                return MessageTypes.Join;
            case "MSG": 
                return MessageTypes.Msg;
            case "BYE": 
                return MessageTypes.Bye;
            case "CONFIRM": 
                return MessageTypes.Confirm;
            case "PING": 
                return MessageTypes.Ping;
        }
        return null;
    }
    
    public MessageTypes? ProcessMsg(string input)
    {
        _savedInput += input;
        if (!input.Contains("\r\n"))
            return null;
        
        string lastStr;
        MessageTypes? msgType;
        do
        {
            //Processes first message and next one can save into _saveInput
            int index = _savedInput.IndexOf("\r\n", StringComparison.Ordinal);
            string str = _savedInput.Substring(0, index + 2);
            if(str.Length != _savedInput.Length)
                _savedInput = _savedInput.Substring(index + 2);
            else
                _savedInput = "";
            
            lastStr = str;
            var match = Regex.Match(str, @"FROM\s+(\S+)\s+IS\s+(.+)");
            
            msgType = DecodeServer_MsgType(str);
            if(msgType == null)
            {
                Console.WriteLine($"ERROR: {input}");
                throw new Exception("Invalid message type");
            }
            switch (msgType)
            {
                case MessageTypes.ReplyOk:
                    Console.Write($"Action Success: {str.Split("IS ")[1]}");
                    break;
                case MessageTypes.ReplyNok:
                    Console.Write($"Action Failure: {str.Split("IS ")[1]}");
                    break;
                case MessageTypes.Msg:
                    Console.WriteLine($"{match.Groups[1].Value}: {match.Groups[2].Value}");
                    break;
                case MessageTypes.Err:
                    Console.WriteLine($"ERROR FROM {match.Groups[1].Value}: {match.Groups[2].Value}");
                    break;
                default:
                    throw new Exception("Income msg processing error");
            }
            
        }while(_savedInput.Contains("\r\n"));
        
        //Defines according to last message
        return msgType;
    }
}