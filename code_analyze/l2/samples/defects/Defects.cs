namespace Samples.Defects;

public static class Defects
{
    public static void ModifyCounters(int count)
    {
        for (int i = 0; i < count; i++)
        {
            i++;
        }

        for (int index = 0; index < count; index++)
        {
            if (index % 2 == 0)
            {
                index += 2;
            }
        }

        for (
            int position = 0;
            position < count;
            position++)
        {
            position = position + 1;
        }
    }
}
