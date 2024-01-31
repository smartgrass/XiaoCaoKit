using UnityEngine;

namespace XiaoCao
{
    public abstract class SettingSo<T> : ScriptableObject
    {
        public T[] moveSettings;

        public T GetSetting(int index)
        {
            if (moveSettings.Length > index)
            {
                return moveSettings[index];
            }

#if UNITY_EDITOR
            if (index == 0)
            {
                Debug.LogError("--- creat one " + this.name);
                moveSettings = new T[1];
            }
#endif
            return moveSettings[0];
        }
    }
}
