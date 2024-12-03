using System;
using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;

namespace MFPC.Utils.SaveLoad
{
    public class PlayerPrefsSaver : ISaver
    {
        private string _globalSaveKey;

        public PlayerPrefsSaver(string globalSaveKey)
        {
            _globalSaveKey = globalSaveKey;
        }

        public ISaver Save<T>(string key, T value)
        {
            if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(GetKey(key), (int) (object) value);
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(GetKey(key), (float) (object) value);
            }
            else if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(GetKey(key), (string) (object) value);
            }
            else if (typeof(T) == typeof(bool))
            {
                int intValue = (bool) (object) value ? 1 : 0;
                PlayerPrefs.SetInt(GetKey(key), intValue);
            }
            else if (typeof(T) == typeof(Vector2))
            {
                Vector2 vectorValue = (Vector2) (object) value;
                string serializedValue = vectorValue.x + ":" + vectorValue.y;
                PlayerPrefs.SetString(GetKey(key), serializedValue);
            }
            else
            {
                if (value is ISerializable)
                {
                    string serializedValue = JsonUtility.ToJson(value);
                    PlayerPrefs.SetString(GetKey(key), serializedValue);
                }
                else
                {
                    throw new Exception("Unsupported data type for PlayerPrefs save: " + typeof(T).ToString());
                }
            }

            PlayerPrefs.Save();

            return this;
        }

        public ISaver Load<T>(string key, Action<T> callback)
        {
            if (!PlayerPrefs.HasKey(GetKey(key))) return this;

            if (typeof(T) == typeof(int))
            {
                int loadedValue = PlayerPrefs.GetInt(GetKey(key));
                callback.Invoke((T) (object) loadedValue);
            }
            else if (typeof(T) == typeof(float))
            {
                float loadedValue = PlayerPrefs.GetFloat(GetKey(key));
                callback.Invoke((T) (object) loadedValue);
            }
            else if (typeof(T) == typeof(string))
            {
                string loadedValue = PlayerPrefs.GetString(GetKey(key));
                callback.Invoke((T) (object) loadedValue);
            }
            else if (typeof(T) == typeof(bool))
            {
                bool boolValue = PlayerPrefs.GetInt(GetKey(key)) != 0;
                callback.Invoke((T) (object) boolValue);
            }
            else if (typeof(T) == typeof(Vector2))
            {
                string serializedValue = PlayerPrefs.GetString(GetKey(key));
                string[] parts = serializedValue.Split(':');

                float loadedX = float.Parse(parts[0]);
                float loadedY = float.Parse(parts[1]);
                Vector2 loadedValue = new Vector2(loadedX, loadedY);
                callback.Invoke((T) (object) loadedValue);
            }
            else
            {
                if (((IList) typeof(T).GetInterfaces()).Contains(typeof(ISerializable)))
                {
                    string serializedValue = PlayerPrefs.GetString(GetKey(key));
                    T loadedValue = JsonUtility.FromJson<T>(serializedValue);
                    callback.Invoke(loadedValue);
                }
                else
                {
                    throw new Exception("Unsupported data type for PlayerPrefs load: " + typeof(T).ToString());
                }
            }

            return this;
        }

        private string GetKey(string localKey)
        {
            return localKey + _globalSaveKey;
        }
    }
}