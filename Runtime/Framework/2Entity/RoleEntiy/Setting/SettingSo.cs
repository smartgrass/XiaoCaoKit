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
            return moveSettings[0];
        }
    }
}
