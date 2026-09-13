using System;

public static class CornerCase
{
    public static int Check(int input, bool printValue)
    {
        int maybeRead = input;

        if (printValue)
        {
            Console.WriteLine(maybeRead);
        }

        int marker = 7;
        string text = "marker";
        Console.WriteLine(text); // marker встречается в комментарии, но это не чтение.

        SetValue(out int output);
        ChangeValue(ref output);
        return output;
    }

    private static void SetValue(out int value)
    {
        value = 40;
    }

    private static void ChangeValue(ref int value)
    {
        value += 2;
    }
}

