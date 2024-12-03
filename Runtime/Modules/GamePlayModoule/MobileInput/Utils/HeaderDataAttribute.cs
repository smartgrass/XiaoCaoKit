using UnityEngine;

namespace MFPC.Utils
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class HeaderData : PropertyAttribute
    {
        public readonly string header;
        
        public HeaderData(string header) => this.header = header;
    }
}