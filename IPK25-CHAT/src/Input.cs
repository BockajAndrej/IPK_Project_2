using IPK25_CHAT.structs;

namespace IPK25_CHAT;

public static class Input
{
    public static bool Parser(string[] args, ref ProgProperty property)
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
        

        if (property.Url == null || property.IsTcp == null)
        {
            return false;
        }
        return true;
    }
    
    //Return value if is input contend command or message (false = message)
    public static bool IsCommand(string input)
    {
        if (input.Split(" ").Length == 0)
            return false;

        if (input == "/help")
            return true;
        
        switch (input.Split(" ")[0])
        {
            case "/auth":
                if(input.Split(" ").Length != 4)
                    return false;
                break;
            case "/join":
                if(input.Split(" ").Length != 2)
                    return false;
                break;
            case "/rename":
                if(input.Split(" ").Length != 2)
                    return false;
                break;
            default:
                return false;
        }
        return true;
    }
    
    public static MessageTypes MsgType(string input)
    {
        if(IsCommand(input))
        {
            if (input.Split(" ")[0] == "/rename")
                return MessageTypes.Rename;

            if (input.Split(" ")[0] == "/auth")
                return MessageTypes.Auth;

            if (input.Split(" ")[0] == "/join")
                return MessageTypes.Join;
            
            if(input == "/help")
                return MessageTypes.Help;
        }
        return MessageTypes.Msg;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage: ipk25-CHAT [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("Usage:");
        Console.WriteLine("  -t <tcp|udp>             Transport protocol used for connection (required)");
        Console.WriteLine("  -s <IP or hostname>      Server IP address or hostname (required)");
        Console.WriteLine("  -p <port>                Server port (default: 4567)");
        Console.WriteLine("  -d <timeout_ms>          UDP confirmation timeout in milliseconds (default: 250)");
        Console.WriteLine("  -r <retry_count>         Maximum number of UDP retransmissions (default: 3)");
        Console.WriteLine("  -h                       Prints program help output and exits");
    }
}