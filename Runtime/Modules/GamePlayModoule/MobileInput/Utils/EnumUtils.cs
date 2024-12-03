using System;
using System.Linq;

namespace MFPC.Utils
{
    public static class EnumUtils
    {
        public static bool IsMaximumSelection<T>(T states) where T : Enum
        {
            int totalFlags = Enum.GetValues(typeof(T))
                .Cast<int>()
                .Sum();
            
            return (int)(object)states == totalFlags;
        }
    }
}