namespace IPK25_CHAT;

public static class Debug
{
    public static bool isEnabled = true;
    public static void StdErrWriteLine(string line)
    {
        if (isEnabled)
            Console.Error.WriteLine(line);
    }
}