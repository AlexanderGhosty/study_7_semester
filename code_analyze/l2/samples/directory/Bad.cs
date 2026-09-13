namespace Samples.Directory;

public static class Bad
{
    public static void Run(int count)
    {
        for (int i = 0; i < count; i++)
        {
            --i;
        }
    }
}
