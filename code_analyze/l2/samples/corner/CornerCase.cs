namespace Samples.Corner;

public static class CornerCase
{
    public static void ModifyByReference(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Mutate(ref i);
        }
    }

    private static void Mutate(ref int value)
    {
        value++;
    }
}
