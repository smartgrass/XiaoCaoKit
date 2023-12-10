using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao {
    public interface ITags {
        public void AddTag(int tag);
        public void RemoveTag(int tag);
        public bool HasTag(int tag);
    }
}