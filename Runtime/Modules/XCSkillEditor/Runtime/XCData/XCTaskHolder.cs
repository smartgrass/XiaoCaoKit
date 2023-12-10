using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public abstract class SubEventHolder : MonoBehaviour
    {
        public abstract List<XCEvent> Events { get; }
    }

    public static class XCEventHelper
    {
        public static void AddList<T>(this List<XCEvent> list, List<T> holderList) where T : XCEvent
        {
            foreach (var item in holderList)
            {
                list.Add(item);
            }
        }
    }

}