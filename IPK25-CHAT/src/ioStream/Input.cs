using System.Text.RegularExpressions;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.ioStream;

public static class Input
{
    private static string _savedInput = "";
    
    public static bool Parser(string[] args, ProgProperty property)
    {
        string? argState = null;
        foreach (string arg in args)
        {
            // Parse arguments
            if (argState == null)
            {
                if (arg == "-h")
                {
                    PrintUsage();
                    return false;
                }
                if (arg.StartsWith('-'))
                {
                    argState = arg;
                }
                else
                    throw new Exception("Argument error: undefined argument");
            }
            else
            {
                if (arg.StartsWith("-"))
                    break;
                switch (argState)
                {
                    case "-t":
                        if(property.IsTcp != null)
                            throw new Exception("Argument error: multiple transport protocols");
                        if (arg != "tcp" && arg != "udp")
                            throw new Exception("Argument error: undefined transport protocol");
                        property.IsTcp = arg == "tcp";
                        break;
                    case "-s":
                        if (property.Url != null)
                            throw new Exception("Argument error: multiple server URLs");
                        property.Url = arg;
                        break;
                    case "-p":
                        if(!int.TryParse(arg, out int port))
                            throw new Exception("Argument error: port is invalid");
                        property.Port = port;
                        break;
                    case "-d":
                        property.Timeout = int.Parse(arg);
                        if(property.Timeout <= 0)
                            throw new Exception("Argument error: wait time must be positive integer");
                        break;
                    case "-r":
                        if(!int.TryParse(arg, out int numberOfRetransmits))
                            throw new Exception("Argument error: number of retransmit is invalid");
                        property.NumberOfRetransmits = numberOfRetransmits;
                        break;
                    default:
                        throw new Exception("Internal error: invalid argument state");
                }
                argState = null;
            }
        }
        

        if (property.Url == null)
        {
            return false;
        }
        return true;
    }
    
    //Return value if is input contend command or message (true = message)
    public static bool GrammarCheck(string input)
    {
        if (input.Split(" ").Length == 0)
            return false;
        
        switch (input.Split(" ")[0])
        {
            case "/auth":
                if(input.Split(" ").Length != 4)
                    throw new Exception("Internal error: invalid grammar state");
                break;
            case "/join":
                if(input.Split(" ").Length != 2)
                    throw new Exception("Internal error: invalid grammar state");
                break;
            case "/rename":
                if(input.Split(" ").Length != 2)
                    throw new Exception("Internal error: invalid grammar state");
                break;
            default:
                return false;
        }
        return true;
    }
    
    public static MessageTypes? SendMsgType(string input, ref UserProperty userProperty)
    {
        if(Input.GrammarCheck(input))
        {
            if (input.Split(" ")[0] == "/rename")
            {
                userProperty.DisplayName = input.Split(" ")[1];
                return null;
            }

            if (input.Split(" ")[0] == "/auth")
            {
                userProperty.Username = input.Split(" ")[1];
                userProperty.Secret = input.Split(" ")[2];
                userProperty.DisplayName = input.Split(" ")[3];
                return MessageTypes.Auth;
            }

            if (input.Split(" ")[0] == "/join")
            {
                userProperty.ChanelId = input.Split(" ")[1];
                return MessageTypes.Join;
            }
        }
        userProperty.MessageContent = input;
        return MessageTypes.Msg;
    }
    private static MessageTypes? IncomeMsgType(string input)
    {
        if (input.Split(" ")[0] == "ERR")
            return MessageTypes.Err;
        if (input.Split(" ")[0] == "REPLY")
        {
            if(input.Split(" ")[1] == "OK")
                return MessageTypes.ReplyOk;
            return MessageTypes.ReplyNok;
        }
        if(input.Split(" ")[0] == "AUTH")
            return MessageTypes.Auth;
        if(input.Split(" ")[0] == "JOIN")
            return MessageTypes.Join;
        if(input.Split(" ")[0] == "MSG")
            return MessageTypes.Msg;
        if(input.Split(" ")[0] == "BYE")
            return MessageTypes.Bye;
        if(input.Split(" ")[0] == "CONFIRM")
            return MessageTypes.Confirm;
        if(input.Split(" ")[0] == "PING")
            return MessageTypes.Ping;
        throw new Exception("OutputMsgType error: Can not recognise message type");
    }
    
    public static MessageTypes? IncomeMsgProcess(string input)
    {
        _savedInput += input;
        if (!input.Contains("\r\n"))
        {
            Debug.WriteLine($"CONTAIN = {_savedInput}");
            return null;
        }

        string lastStr = "";
        foreach (var str in _savedInput.Split("\r\n"))
        {
            if(str.Length > 0)
            {
                lastStr = str;
                var match = Regex.Match(str, @"FROM\s+(\S+)\s+IS\s+(.+)");
                switch (IncomeMsgType(str))
                {
                    case MessageTypes.ReplyOk:
                        Console.WriteLine($"Action Success: {str.Split("IS ")[1]}");
                        break;
                    case MessageTypes.ReplyNok:
                        Console.WriteLine($"Action Failure: {str.Split("IS ")[1]}");
                        break;
                    case MessageTypes.Msg:
                        Console.WriteLine($"{match.Groups[1].Value}: {match.Groups[2].Value}");
                        break;
                    case MessageTypes.Err:
                        Console.WriteLine($"ERROR FROM {match.Groups[1].Value}: {match.Groups[1].Value}");
                        break;
                    default:
                        throw new Exception("Income msg processing error");
                }
            }
        }
        _savedInput = "";
        return IncomeMsgType(lastStr);
    }
    
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: ipk25-CHAT [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Options:");
    }
}