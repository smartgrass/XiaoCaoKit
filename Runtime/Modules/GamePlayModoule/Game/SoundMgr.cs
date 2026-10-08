using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using XiaoCao.UI;

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
            SceneManager.sceneLoaded += OnSceneLoaded;
            ReadSettting();
            PlaySettingBgm();


        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 场景加载完成后重新读取该场景的bgm配置并播放
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
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
            SetVolume(group, ConfigMgr.Inst.LocalSetting.GetValue(group, defaultValue));
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

            if (!ConfigMgr.Inst.SoundCfg.IsHas(tag))
            {
                return;
            }

            var array = ConfigMgr.Inst.SoundCfg.Dic[tag];
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

        #region Bgm

        /// <summary>默认选项标识: 播放sound.ini中 Scene_{场景名} 的配置</summary>
        public const string BgmDefaultValue = "Default";
        /// <summary>无选项标识: 不播放</summary>
        public const string BgmNoneValue = "None";

        /// <summary>
        /// bgm下拉选项: Display为显示文本(预设项按当前语言本地化), Value为保存配置用的稳定标识
        /// </summary>
        public class BgmOption
        {
            public string Display;
            public string Value;

            public BgmOption(string display, string value)
            {
                Display = display;
                Value = value;
            }
        }

        /// <summary>
        /// 场景bgm配置保存Key, 格式 Bgm_{场景名}, 用于区分不同场景选择的bgm
        /// </summary>
        public static string GetBgmSceneKey(string sceneName)
        {
            return $"{LocalizeKey.Bgm}_{sceneName}";
        }

        /// <summary>
        /// 获取bgm下拉选项: 预设的默认/无 + GameConfig/Bgm各子目录下的mp3
        /// bgm选项显示格式: 文件名(子目录名), 如 Home_1(Main)
        /// </summary>
        public static List<BgmOption> GetBgmOptions()
        {
            List<BgmOption> options = new List<BgmOption>
            {
                new BgmOption(LocalizeKey.BgmDefault.ToLocalizeStr(), BgmDefaultValue),
                new BgmOption(LocalizeKey.BgmNone.ToLocalizeStr(), BgmNoneValue),
            };

            DirectoryInfo root = new DirectoryInfo($"{XCPathConfig.GetGameConfigDir()}/Bgm");
            if (!root.Exists)
            {
                return options;
            }

            foreach (DirectoryInfo subDir in root.GetDirectories().OrderBy(d => d.Name))
            {
                foreach (FileInfo file in subDir.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).OrderBy(f => f.Name))
                {
                    //文件名不带扩展名, 括号内为子目录名
                    string showName = $"{Path.GetFileNameWithoutExtension(file.Name)}({subDir.Name})";
                    options.Add(new BgmOption(showName, showName));
                }
            }

            return options;
        }

        /// <summary>
        /// 将下拉选项标识解析为bgm文件的绝对路径, 预设选项返回false
        /// </summary>
        public static bool TryGetBgmPath(string option, out string path)
        {
            path = null;
            if (string.IsNullOrEmpty(option))
            {
                return false;
            }

            int left = option.LastIndexOf('(');
            int right = option.LastIndexOf(')');
            if (left <= 0 || right != option.Length - 1)
            {
                return false;
            }

            string fileName = option.Substring(0, left);
            string subDir = option.Substring(left + 1, right - left - 1);
            path = $"{XCPathConfig.GetGameConfigDir()}/Bgm/{subDir}/{fileName}.mp3";
            return true;
        }

        /// <summary>
        /// 播放当前场景的bgm: 读取该场景已保存的选择;
        /// 默认 -> 读取sound.ini的Scene_{场景名}; 无 -> 停止播放;
        /// 其他 -> 播放所选文件, 文件不存在时回退到默认逻辑
        /// </summary>
        public void PlaySettingBgm()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string option = GetBgmSceneKey(sceneName).GetKeyString();
            if (string.IsNullOrEmpty(option))
            {
                option = BgmDefaultValue;
            }

            if (option == BgmNoneValue)
            {
                StopBgm();
                return;
            }

            if (option != BgmDefaultValue && TryGetBgmPath(option, out string fullPath))
            {
                if (File.Exists(fullPath))
                {
                    PlayBgmByPath(fullPath);
                    return;
                }

                //存档的bgm文件被删除或改名, 回退到默认逻辑
                Debug.LogWarning($"--- saved bgm file not found: {fullPath}");
            }

            PlaySceneCfgBgm(sceneName);
        }

        /// <summary>
        /// 读取sound.ini中 Scene_{场景名} 段配置的bgm并播放, 未配置则不播放
        /// </summary>
        private void PlaySceneCfgBgm(string sceneName)
        {
            InitArrayFile soundCfg = ConfigMgr.Inst.SoundCfg;
            string section = $"Scene_{sceneName}";
            if (soundCfg == null || soundCfg.Dic == null || !soundCfg.IsHas(section))
            {
                StopBgm();
                return;
            }

            List<string> pathList = soundCfg.Dic[section];
            if (pathList == null || pathList.Count == 0)
            {
                StopBgm();
                return;
            }

            //配置的bgm文件不存在时视同未配置
            string fullPath = $"{XCPathConfig.GetGameConfigDir()}/{pathList[0]}";
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"--- bgm file not found: {fullPath}");
                StopBgm();
                return;
            }

            PlayBgmByPath(fullPath);
        }

        /// <summary>
        /// 播放指定绝对路径的mp3文件
        /// </summary>
        public void PlayBgmByPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                StopBgm();
                return;
            }

            StartCoroutine(LoadAndPlayMP3(filePath));
        }

        /// <summary>
        /// 停止当前bgm
        /// </summary>
        public void StopBgm()
        {
            musicAs.Stop();
        }

        /// <summary>
        /// 播放扫描到的第一个bgm
        /// </summary>
        public void PlayFristBgm()
        {
            foreach (BgmOption option in GetBgmOptions())
            {
                if (TryGetBgmPath(option.Value, out string path))
                {
                    PlayBgmByPath(path);
                    return;
                }
            }
        }

        #endregion


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
