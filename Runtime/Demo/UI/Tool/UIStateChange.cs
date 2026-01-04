using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCaoKit.UI
{
    public class UIStateChange : MonoBehaviour
    {
        public string desc;
        public List<UIGroup> uiGroups = new List<UIGroup>();
        public int state;


        [Button]
        private void TestState()
        {
            ApplyState();
        }


        public void SetState(int newState)
        {
            if (newState < 0 || newState >= uiGroups.Count)
            {
                Debug.LogWarning($"UIStateChange: state {newState} out of range");
                return;
            }

            if (state == newState)
            {
                return;
            }

            state = newState;
            ApplyState();
        }

        private void ApplyState()
        {
            if (state < 0 || state >= uiGroups.Count)
            {
                return;
            }

            for (int i = 0; i < uiGroups.Count; i++)
            {
                if (i == state)
                {
                    continue;
                }

                ToggleGroup(uiGroups[i], false);
            }

            ToggleGroup(uiGroups[state], true);
        }

        private static void ToggleGroup(UIGroup group, bool shouldShow)
        {
            if (group == null || group.objects == null)
            {
                return;
            }

            foreach (GameObject go in group.objects)
            {
                if (go != null && go.activeSelf != shouldShow)
                {
                    go.SetActive(shouldShow);
                }
            }
        }
    }

    [System.Serializable]
    public class UIGroup
    {
        public List<GameObject> objects = new List<GameObject>();
    }
}
