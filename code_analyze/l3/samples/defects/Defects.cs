using System;

public static class Defects
{
    public static int Calculate(int input)
    {
        int neverRead = 10;

        int overwritten = input + 1;
        overwritten = input + 2;

        int lastWrite = input * 3;
        Console.WriteLine(lastWrite);
        lastWrite = 0;

        SetValue(out int output);

        int incremented = input;
        incremented++;

        return overwritten;
    }

    private static void SetValue(out int value)
    {
        value = 42;
    }
}
