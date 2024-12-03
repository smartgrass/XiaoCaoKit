using UnityEngine;

namespace MFPC.Utils
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class CenterHeader : PropertyAttribute
    {
        public readonly string Header;
        public readonly float Height;
        public readonly float Spacing;

        public CenterHeader(string header, float height = 2, float spacing = 10)
        {
            Height = height;
            Spacing = spacing;
            Header = header;
        }
    }
}