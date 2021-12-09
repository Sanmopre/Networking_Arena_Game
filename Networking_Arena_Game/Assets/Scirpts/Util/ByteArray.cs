public class ByteArray
{
    public static bool Compare(byte[] array1, byte[] array2, int index = 0, int count = 0)
    {
        if ((count == 0 && array1.Length - index != array2.Length) || 
            (array1.Length - index < count || array2.Length < count))
            return false;
        if (count == 0)
            count = array1.Length - index;

        for (int i = index; i < count; ++i)
            if (array1[i] != array2[i])
                return false;
        return true;
    }

    public static byte[] TrimEnd(byte[] array)
    {
        if (array.Length == 0)
            return array;

        int i;
        for (i = array.Length - 1; i >= 0; --i)
            if (array[i] != '\0')
            {
                ++i;
                break;
            }

        int exces = array.Length - (array.Length - i);
        if (exces == array.Length)
            return array;
        byte[] newArray = new byte[exces];

        for (i = 0; i < newArray.Length; ++i)
            newArray.SetValue(array[i], i);
        return newArray;
    }

    public static int PushBack(ref byte[] dest, byte[] from, int index)
    {
        int i;
        for (i = 0; i < from.Length; ++i)
            dest.SetValue(from[i], index + i);
        return index + i;
    }
}
