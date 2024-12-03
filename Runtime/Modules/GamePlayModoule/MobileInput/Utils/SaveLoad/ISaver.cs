using System;
using System.Runtime.Serialization;

namespace MFPC.Utils.SaveLoad
{
    public interface ISaver
    {
        ISaver Save<T>(string key, T value);
        ISaver Load<T>(string key, Action<T> callback);
    }
}