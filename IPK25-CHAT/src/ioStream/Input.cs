using IPK25_CHAT.structs;

namespace IPK25_CHAT.ioStream;

public class Input
{
    //Using ref not out bucause of default values
    public bool Parser(string[] args, ProgProperty property)
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

    void PrintUsage()
    {
        Console.WriteLine("Usage: ipk25-CHAT [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Options:");
    }
}