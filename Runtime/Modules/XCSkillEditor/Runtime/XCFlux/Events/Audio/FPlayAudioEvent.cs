using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using XiaoCao;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Flux
{
    [FEvent("Audio/Play Audio")]
    public class FPlayAudioEvent : FEvent
    {
        [SerializeField]
        private AudioClip _audioClip = null;
        public AudioClip AudioClip { get { return _audioClip; } }

        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 1f;

        [SerializeField]
        private bool _loop = false;
        public bool Loop { get { return _loop; } }

        [SerializeField]
        [HideInInspector]
        private int _startOffset = 0;
        public int StartOffset { get { return _startOffset; } }

        [SerializeField]
        private bool _speedDeterminesPitch = true;
        public bool SpeedDeterminesPitch { get { return _speedDeterminesPitch; } set { _speedDeterminesPitch = value; } }

        //public static 

        public static MethodInfo PlayPreviewClipMethod;
        public static void PlayEditorAudioClip(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                return;
            }
            if (PlayPreviewClipMethod == null)
            {
#if UNITY_EDITOR
                var unityEditorAssembly = typeof(AudioImporter).Assembly;
                var audioUtil = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                BindingFlags all = (BindingFlags)~BindingFlags.Default;
                PlayPreviewClipMethod = audioUtil.GetMethod("PlayPreviewClip", all);
#endif
            }
            PlayPreviewClipMethod.Invoke(null, new object[] { audioClip,0,false});
        }

        protected override void OnTrigger(float timeSinceTrigger)
        {
            if (Sequence.isPlayClip&& Sequence.IsPlayingForward)
            {
                PlayEditorAudioClip(_audioClip);
            }
            //         _source = Owner.GetComponent<AudioSource>();
            //if( _source == null )
            //	_source = Owner.gameObject.AddComponent<AudioSource>();
            //_source.volume = _volume;
            //_source.loop = _loop;
            //_source.clip = _audioClip;

            //if( Sequence.IsPlaying )
            //	_source.Play();
            //_source.time = _startOffset * Sequence.InverseFrameRate + timeSinceTrigger;
            //if( SpeedDeterminesPitch )
            //	_source.pitch = Sequence.Speed * Time.timeScale;
        }

        protected override void OnPause()
        {
            //_source.Pause();
        }

        protected override void OnResume()
        {
            //if( Sequence.IsPlaying )
            //	_source.Play();
        }

        protected override void OnFinish()
        {
            //if( _source.clip == _audioClip && _source.isPlaying )
            //{
            //	_source.Stop();
            //	_source.clip = null;
            //}
        }

        protected override void OnStop()
        {
            //if( _source.clip == _audioClip && _source.isPlaying )
            //{
            //	_source.Stop();
            //	_source.clip = null;
            //}
        }

        public override int GetMaxLength()
        {
            if (_loop || _audioClip == null)
                return base.GetMaxLength();

            return Mathf.RoundToInt(_audioClip.length * Sequence.FrameRate);
        }

        public int GetMaxStartOffset()
        {
            if (_audioClip == null)
                return 0;

            int maxFrames = Mathf.RoundToInt(_audioClip.length * Sequence.FrameRate);

            if (_loop)
                return maxFrames;

            return maxFrames - Length;
        }

        public override string Text
        {
            get
            {
                return _audioClip == null ? "!Missing!" : _audioClip.name;
            }
            set
            {

            }
        }

        public override XCEvent ToXCEvent()
        {
#if UNITY_EDITOR
            XCAudioEvent audioEvent = new XCAudioEvent();
            //Path.GetFileName(
            audioEvent.eName = AssetDatabase.GetAssetPath(_audioClip);
            audioEvent.volume = _volume;
            audioEvent.isPlayerOnly = IsPlayerOnly;
            Debug.Log($"--- {audioEvent.eName}");
            return audioEvent;
#else
            return base.ToXCEvent();
#endif
        }
    }

}
