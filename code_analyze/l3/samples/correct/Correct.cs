using System;

public static class Correct
{
    public static int Calculate(int input, bool increase)
    {
        int result;

        if (increase)
        {
            result = input + 1;
        }
        else
        {
            result = input - 1;
        }

        int multiplier = 2;
        result += multiplier;

        for (int index = 0; index < input; index++)
        {
            result += index;
        }

        Console.WriteLine(result);
        return result;
    }
}
