namespace Samples.Correct;

public static class Correct
{
    public static int Sum(int count)
    {
        int result = 0;

        for (int i = 0; i < count; i++)
        {
            result += i;
            string example = "i++ is only text";
            Console.WriteLine(example); // i += 2 is only a comment
        }

        return result;
    }

    public static void UpdateMember(Counter item)
    {
        for (int i = 0; i < 3; ++i)
        {
            item.i++;
        }
    }

    public sealed class Counter
    {
        public int i;
    }
}
