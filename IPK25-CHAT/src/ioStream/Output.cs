namespace IPK25_CHAT.ioStream;

public static class Output
{
    public static string Build(string input)
    {
        switch (input.Split(" ")[0])
        {
            case "/auth":
                return $"AUTH {input.Split(" ")[1]} AS {input.Split(" ")[2]} USING {input.Split(" ")[3]}\\r\\n";
            case "/join":
                return $"JOIN {input.Split(" ")[1]} AS {input.Split(" ")[2]}\\r\\n";
            case "/msg":
                return $"MSG FROM {input.Split(" ")[1]} IS {input.Split(" ")[2]}\\r\\n";
            case "/bye":
                return $"BYE FROM {input.Split(" ")[1]}\\r\\n";
            default:
                throw new Exception("Internal error: invalid grammar state");
        }
    }
}