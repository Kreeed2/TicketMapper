namespace SyncApp.Helper;

public static class Utilities
{
    public static void AddIfNotNull(Dictionary<string, object> pDictionary, string pKey, object? pValue)
    {
        if (pValue is not null)
        {
            pDictionary[pKey] = pValue;
        }
    }
}
