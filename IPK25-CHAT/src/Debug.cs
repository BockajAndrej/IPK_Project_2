﻿namespace IPK25_CHAT;

public static class Debug
{
    public static bool IsEnabled = false;
    public static void WriteLine(string line)
    {
        if (IsEnabled)
            Console.Error.WriteLine(line);
    }
}