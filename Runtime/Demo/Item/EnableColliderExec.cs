using UnityEngine;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class EnableColliderExec : MonoExecute
    {
        public bool isEnable = false;

        public bool checkChild;

        public override void Execute()
        {
            if (checkChild)
            {
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent<Collider>(out var childCol))
                    {
                        childCol.enabled = isEnable;
                    }
                }
            }
            else
            {
                if (transform.TryGetComponent<Collider>(out var col))
                {
                    col.enabled = isEnable;
                }
            }
        }
    }
}