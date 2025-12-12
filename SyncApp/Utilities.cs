using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncApp;

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
