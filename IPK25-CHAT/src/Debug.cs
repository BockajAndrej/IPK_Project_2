namespace IPK25_CHAT;

public static class Debug
{
    public static bool isEnabled = false;
    public static void WriteLine(string line)
    {
        if (isEnabled)
            Console.Error.WriteLine(line);
    }
}