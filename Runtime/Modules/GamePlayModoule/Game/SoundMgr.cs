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

        /// <summary>界面"操作成功"提示音的 sound.ini 段名(技能升级、武器锻造等)</summary>
        public const string UISureSoundKey = "Success";

        /// <summary>
        /// 播放界面"操作成功"提示音: 技能升级成功、武器锻造成功等需要给玩家明确正反馈的操作。
        /// 音频取自 sound.ini 的 Success 段, 走 sfx 通道, 受音效音量设置控制
        /// </summary>
        public void PlayUISure()
        {
            PlayCfgSound(UISureSoundKey);
        }

        /// <summary>关卡通关结算音的 sound.ini 段名</summary>
        public const string LevelWinSoundKey = "LevelWin";

        /// <summary>
        /// 播放关卡通关结算音: 打开关卡结算面板且结果为通关时播放。
        /// 音频取自 sound.ini 的 LevelWin 段, 走 sfx 通道, 受音效音量设置控制
        /// </summary>
        public void PlayLevelWin()
        {
            PlayCfgSound(LevelWinSoundKey);
        }

        /// <summary>
        /// 播放 sound.ini 中指定段的音效, 段内配置多条时随机取一条; 未配置或找不到资源则不播放
        /// </summary>
        public void PlayCfgSound(string soundKey, float volume = 1)
        {
            if (string.IsNullOrEmpty(soundKey))
            {
                return;
            }

            InitArrayFile soundCfg = ConfigMgr.Inst.SoundCfg;
            if (soundCfg == null || soundCfg.Dic == null || !soundCfg.IsHas(soundKey))
            {
                return;
            }

            List<string> pathList = soundCfg.Dic[soundKey];
            if (pathList == null || pathList.Count == 0)
            {
                return;
            }

            PlayClip(pathList[Random.Range(0, pathList.Count)], volume);
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
        /// <summary>随机选项标识: 在 Bgm/Custom 目录里随机播放</summary>
        public const string BgmCustomValue = "Custom";

        /// <summary>随机播放所用的bgm子目录名</summary>
        public const string BgmCustomDirName = "Custom";

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
        /// 获取bgm下拉选项: 预设的默认/无/随机 + GameConfig/Bgm各子目录下的mp3
        /// bgm选项显示格式: 文件名(子目录名), 如 Home_1(Main);
        /// Custom 目录是玩家自定义曲库, 只输出一个"随机"项, 不逐首展开
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

            //Custom 曲库有歌时才提供随机项, 空目录不显示无效选项
            if (GetCustomBgmPaths().Count > 0)
            {
                options.Add(new BgmOption(LocalizeKey.BgmCustom.ToLocalizeStr(), BgmCustomValue));
            }

            foreach (DirectoryInfo subDir in root.GetDirectories().OrderBy(d => d.Name))
            {
                if (subDir.Name == BgmCustomDirName)
                {
                    continue;
                }

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
        /// 扫描 Bgm/Custom 目录下的所有mp3, 返回绝对路径(按文件名排序)
        /// </summary>
        public static List<string> GetCustomBgmPaths()
        {
            List<string> paths = new List<string>();
            DirectoryInfo dir = new DirectoryInfo($"{XCPathConfig.GetGameConfigDir()}/Bgm/{BgmCustomDirName}");
            if (!dir.Exists)
            {
                return paths;
            }

            foreach (FileInfo file in dir.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).OrderBy(f => f.Name))
            {
                paths.Add(file.FullName);
            }

            return paths;
        }

        /// <summary>上一次随机到的Custom曲目, 用于避免连续两次播同一首</summary>
        private string _lastCustomBgmPath;

        /// <summary>
        /// 从 Bgm/Custom 随机取一首; 有多首时排除上一次播过的那首。
        /// 目录为空返回 null
        /// </summary>
        public string GetRandomCustomBgmPath()
        {
            List<string> paths = GetCustomBgmPaths();
            if (paths.Count > 1)
            {
                paths.Remove(_lastCustomBgmPath);
            }

            if (paths.Count == 0)
            {
                return null;
            }

            _lastCustomBgmPath = paths[Random.Range(0, paths.Count)];
            return _lastCustomBgmPath;
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
        /// 随机 -> 在 Bgm/Custom 中随机播放一首;
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

            if (option == BgmCustomValue)
            {
                string customPath = GetRandomCustomBgmPath();
                if (!string.IsNullOrEmpty(customPath))
                {
                    PlayBgmByPath(customPath);
                    return;
                }

                //曲库被清空时回退到默认逻辑
                Debug.LogWarning($"--- bgm {BgmCustomDirName} dir is empty");
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

            //同场景配置多条bgm时随机取一条
            string bgmName = pathList[Random.Range(0, pathList.Count)];

            //配置的bgm文件不存在时视同未配置
            string fullPath = $"{XCPathConfig.GetGameConfigDir()}/{bgmName}";
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
