public static class Bad
{
    public static int Identity(int value)
    {
        int unused = value * 2;
        return value;
    }
}

