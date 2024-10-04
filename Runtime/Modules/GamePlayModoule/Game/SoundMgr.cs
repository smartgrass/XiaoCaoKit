using UnityEngine;
using UnityEngine.Audio;

namespace XiaoCao
{
    public class SoundMgr : MonoSingletonPrefab<SoundMgr>, IMgr
    {
        public AudioMixer mixer;
        [Header("master,music,sfx")]
        public AudioMixerGroup[] mixerGroups;

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
            if (string.IsNullOrEmpty(soundId))
            {
                return;
            }

            string path = XCPathConfig.GetAudioPath(soundId);
            AudioClip audioClip = ResMgr.LoadAseet(path) as AudioClip;
            AudioSource source = asPool.CheckGet(out bool isNew);
            if (isNew)
            {
                source.outputAudioMixerGroup = mixerGroups[2];
                source.transform.SetParent(transform);
            }
            source.clip = audioClip;
            source.volume = volume;
            source.Play();
        }



        public void PlayHitAudio(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            //Default,Sword,Break

            if (!ConfigMgr.SoundCfg.IsHas(tag))
            {
                return;
            }

            var array = ConfigMgr.SoundCfg.Dic[tag];
            int count = array.Count;
            var path = array[Random.Range(0, count)];


            AudioClip audioClip = ResMgr.LoadAseet(path) as AudioClip;
            AudioSource source = hitAsPool.CheckGet(out bool isNew);
            if (isNew)
            {
                source.outputAudioMixerGroup = mixerGroups[2];
                source.transform.SetParent(transform);
            }
            source.clip = audioClip;
            source.volume = 1;
            source.Play();
        }
    }

}