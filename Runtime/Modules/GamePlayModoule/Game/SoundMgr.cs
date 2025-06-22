using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace XiaoCao
{
    public class SoundMgr : MonoSingletonPrefab<SoundMgr>, IMgr
    {
        public AudioMixer mixer;
        [Header("master,music,sfx")]
        public AudioMixerGroup[] mixerGroups;

        private LoopPool<AudioSource> asPool;
        private LoopPool<AudioSource> hitAsPool;
        private AudioSource musicAs;

        public override void Init()
        {
            base.Init();
            _instance = this;
            GameObject prefabGO = new GameObject("AudioSource");
            prefabGO.transform.SetParent(transform, true);
            AudioSource prefab = prefabGO.AddComponent<AudioSource>();

            asPool = new LoopPool<AudioSource>(prefab, 5, transform);
            hitAsPool = new LoopPool<AudioSource>(prefab, 5, transform);
            musicAs = Instantiate(prefab, transform);
            musicAs.outputAudioMixerGroup = mixerGroups[1];
            musicAs.loop = true;
            ReadSettting();
            PlaySettingBgm();


        }

        void ReadSettting()
        {
            ReadGroupSetting(SoundPanel.MainVolumeKey);
            ReadGroupSetting(SoundPanel.MusicVolumeKey, 0.5f);
            ReadGroupSetting(SoundPanel.SFXVolumeKey);
        }

        void ReadGroupSetting(string group,float defaultValue = 1)
        {
            SetVolume(group, ConfigMgr.LocalSetting.GetValue(group, defaultValue));
        }

        ///<see cref="SoundPanel.MainVolumeKey"/>
        ///输入参数为 0~1
        public void SetVolume(string group, float volume)
        {
            volume = Remap01ToDB(volume);
            mixer.SetFloat(group, volume);
        }

        private float Remap01ToDB(float x)
        {
            if (x <= 0.0f) x = 0.0001f;
            return Mathf.Log10(x) * 20.0f;
        }

        public void PlayClip(AudioClip audioClip, float volume = 1)
        {
            if (audioClip != null)
            {
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

        //根据文件名播放歌曲
        public void PlayBgmByName(string fileName)
        {
            if (fileName == "" || fileName == "--")
            {
                musicAs.Stop();
                return;
            }
            string filePath = $"{XCPathConfig.GetGameConfigDir()}/Bgm/{fileName}";

            StartCoroutine(LoadAndPlayMP3(filePath));
        }

        public void PlaySettingBgm()
        {
            string bgm = LocalizeKey.Bgm.GetKeyString();
            if (bgm == "")
            {
                PlayFristBgm();
            }
            else
            {
                PlayBgmByName(bgm);
            }
        }

        public void PlayFristBgm()
        {
            DirectoryInfo directory = new DirectoryInfo($"{XCPathConfig.GetGameConfigDir()}/Bgm");
            var files = directory.GetFiles("*.mp3", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                StartCoroutine(LoadAndPlayMP3(files[0].FullName));
            }
        }

        // 从Android平台的StreamingAssets加载BGM
        IEnumerator LoadBgmFromAndroid(string dirName)
        {
            // 注意：在Android上，StreamingAssets路径需要使用"jar:file://"前缀
            string url = "jar:file://" + dirName;

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // 处理成功获取的文件
                    Debug.Log("成功获取BGM目录内容");
                    // 注意：这里返回的是整个目录的内容，需要进一步解析
                    //ProcessBgmFiles(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("获取BGM目录失败: " + www.error);
                }
            }
        }


        private IEnumerator LoadAndPlayMP3(string filePath)
        {
            Debug.Log($"--- LoadAndPlayMP3 {filePath}");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
            {
                if (www.result != UnityWebRequest.Result.ConnectionError && www.result != UnityWebRequest.Result.ProtocolError)
                {
                    yield return www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError(www.error);
                    }
                    else
                    {
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                        musicAs.clip = audioClip;
                        musicAs.Play();
                    }
                }

                else
                {
                    Debug.LogError("Invalid URL or file path: " + filePath);
                }

            }

        }

        internal void OnReloadScene()
        {
            if (musicAs.isPlaying) {
                musicAs.Stop();
                musicAs.Play();
            }
        }
    }

}