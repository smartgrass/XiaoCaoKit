using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    public class SoundMgr : MonoSingleton<SoundMgr>, IMgr
    {

        public AudioSource audioSourcePrefab;
        private LoopPool<AudioSource> asPool;
        private LoopPool<AudioSource> hitAsPool;

        public override void Init()
        {
            base.Init();
            _instance = this;
            GameObject prefabGO = new GameObject("AudioSource");
            AudioSource prefab = prefabGO.AddComponent<AudioSource>();

            asPool = new LoopPool<AudioSource>(prefab, 5);
            hitAsPool = new LoopPool<AudioSource>(prefab, 5);
        }

        public void PlayClip(string soundId, float volume = 1)
        {
            string path = XCPathConfig.GetAudioPath(soundId);
            AudioClip audioClip = ResMgr.LoadAseet(path) as AudioClip;
            AudioSource source = asPool.Get();
            source.transform.SetParent(transform);
            source.clip = audioClip;
            source.volume = volume;
            source.Play();
        }

        public void PlayHitAudio(int id, bool isBreak)
        {
            if (id == 0) 
                return;

            string soundId = isBreak ? id + "Break" : id.ToString();
            AudioClip audioClip = ResMgr.LoadAseet(soundId) as AudioClip;
            AudioSource source = hitAsPool.Get();
            source.transform.SetParent(transform);
            source.clip = audioClip;
            source.volume = 1;
            source.Play();
        }
    }

}