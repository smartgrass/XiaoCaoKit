//#define FLUX_DEBUG
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.UI.GridLayoutGroup;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Flux
{
	/// @brief Cache for FAnimationTrack
	public class FAnimationTrackCache : FTrackCache {
		
		private Animator _animator = null;
		public Animator Animator
		{
			get {
				if( _animator == null )
					_animator = GetAnimator();
                return _animator;
			}
		}

        Animator GetAnimator()
        {
            Animator animator = Track.Owner.GetComponentInChildren<Animator>();
            if (animator == null)
            {
	            Debug.Log($"-- no Animator in {Track.Owner}");
                // Track.Owner.gameObject.AddComponent<Animator>();
            }
            return animator;
        }

        // @note, can't just check Animator.playbackTime >= 0 because unity prints a warning if it's not in playback :(
        private bool _inPlayback = false;

		// 正在执行 BuildInternal 中（sequence.Init 会递归触发 CreateCache 重入）
		private bool _isBuilding = false;
		/// @brief Is the cache in playback?
    	public bool InPlayback { get { return _inPlayback; } }

		// all the tracks it is caching
		private List<FAnimationTrack> _tracksCached = new List<FAnimationTrack>();

		/// @brief Number of tracks being cached
		public int NumberTracksCached { get { return _tracksCached.Count; } }

		private List<FrameRange> _validFrameRanges = new List<FrameRange>();

		public FAnimationTrackCache( FTrack track )
			:base( track )
		{
		}

		protected override bool BuildInternal()
		{
			// BuildCache 里会调用 sequence.Init()，而 Init 又会触发 CreateTrackCaches，
			// 对同一个 cache 再走一次 CreateCache，形成重入：
			// 外层还没 StartRecording，内层就已经在 Animator 上 Play/Update，
			// Unity 便会报 "Can't playback from recorder, no recorded data found"
			if( _isBuilding )
			{
				string ownerName = Track != null && Track.Owner != null ? Track.Owner.name : "null";
				Debug.LogWarning( $"[Flux] FAnimationTrackCache 正在构建，忽略重入 owner={ownerName}" );
				return false;
			}

			_isBuilding = true;
			try
			{
				return BuildCache();
			}
			finally
			{
				_isBuilding = false;
			}
		}

		private bool BuildCache()
		{
			// 录制前必须退出 playback 模式：上一次预览可能把 Animator 留在 playback 状态，
			// 而 sequence.Init() 重入构建其它轨时会直接 Animator.Update，触发同样的报错
			StopPlayback();

			double recordMs = 0d;
			List<FTrack> tracksToUpdate = GetTracksToUpdate( out _tracksCached );

			_validFrameRanges.Clear();

			if( _tracksCached.Count == 0 || Animator == null || Animator.runtimeAnimatorController == null )
				return false;

			// build preview
#if FLUX_DEBUG
			Debug.LogWarning("Creating Preview");
#endif

			TransformSnapshot transformSnapshot = new TransformSnapshot(Track.Owner);

			FSequence sequence = Track.Sequence;

			if( _tracksCached[0].Snapshot != null )
				_tracksCached[0].Snapshot.Restore();

			bool ownerIsActive = Track.Owner.gameObject.activeSelf;

			float speed = sequence.Speed;
			if( speed == 0 )
				sequence.Speed = 1f;

			// Animator.speed 为负只有在 recorder 启用时才合法，
			// 录制前直接赋负值 Unity 会拒绝并报 "Animator.speed can only be negative..."
			Animator.speed = Mathf.Abs( sequence.Speed );

            int currentFrame = sequence.CurrentFrame;
			bool isPlaying = sequence.IsPlaying;
			
			if( !sequence.IsStopped )
				sequence.Stop();
			
			if( !sequence.IsInit )
				sequence.Init();

			if( !ownerIsActive )
			{
				HideFlags hideFlags = Track.Owner.gameObject.hideFlags;
				Track.Owner.gameObject.hideFlags |= HideFlags.DontSave;
				Track.Owner.gameObject.SetActive( true );
				Track.Owner.gameObject.hideFlags = hideFlags;
			}

			FrameRange currentFrameRange = new FrameRange();

			foreach( FAnimationTrack animTrack in _tracksCached )
			{
				animTrack.Cache = null;
				animTrack.Stop(); // we need to force stop them to clear the currentEvent index
			}
				
			// set culling to get around Unity bug of recording at the start
			AnimatorCullingMode animatorCullingMode = Animator.cullingMode;
			Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; 

			Animator.enabled = true;

			for( int i = 0; i != Animator.layerCount; ++i )
			{
				Animator.SetLayerWeight(i, 0f);
			}

			int processedFrames = 0;
			float recordDelta = 1f / sequence.FrameRate;
			long recordStartTimestamp = Stopwatch.GetTimestamp();
			Animator.StartRecording( -1 );

			bool success = Animator.recorderMode == AnimatorRecorderMode.Record;

			if( success )
			{
				Animator.enabled = false;

				int frame = 0;
				
				while( frame <= sequence.Length )
				{
					++processedFrames;
					bool wasEnabled = Animator.enabled;

					foreach( FTrack track in tracksToUpdate )
						track.UpdateEvents( frame, frame * recordDelta );

					if( wasEnabled )
					{
						currentFrameRange.End += 1;
						if( Animator.enabled )
						{
							Animator.Update( recordDelta );
						}
						else
						{
							Animator.enabled = true;
							//结束帧
							Animator.Update( recordDelta );
							Animator.enabled = false;
							_validFrameRanges.Add( currentFrameRange );
						}

					}
					else if( Animator.enabled )
					{
						Animator.Update( 0 );
						currentFrameRange = new FrameRange( frame, frame );
					}

					++frame;
				}
				
				foreach( FAnimationTrack animTrack in _tracksCached )
				{
					animTrack.Cache = this;
				}

				Track = _tracksCached[0];
				
				Animator.StopRecording();
			}
			recordMs = GetElapsedMilliseconds( recordStartTimestamp );

			if( !ownerIsActive )
			{
				HideFlags hideFlags = Track.Owner.gameObject.hideFlags;
				Track.Owner.gameObject.hideFlags |= HideFlags.DontSave;
				Track.Owner.gameObject.SetActive( false );
				Track.Owner.gameObject.hideFlags = hideFlags;
			}

			if( speed != 1 )
				sequence.Speed = speed;

			Animator.cullingMode = animatorCullingMode;

			sequence.Stop(true);

			if( currentFrame >= 0 )
			{
				if( isPlaying )
					sequence.Play( currentFrame );
				else
					sequence.SetCurrentFrame( currentFrame );

				transformSnapshot.Restore();
			}

			string ownerName = Track != null && Track.Owner != null ? Track.Owner.name : "null";
			string sequenceName = sequence != null ? sequence.name : "null";
			int updateCalls = processedFrames * tracksToUpdate.Count;
			Debug.Log(
				$"[FluxPerf] FAnimationTrackCache.BuildInternal record owner={ownerName}, sequence={sequenceName}, " +
				$"success={success}, frames={processedFrames}, frameRate={sequence.FrameRate}, delta={recordDelta:F4}, " +
				$"updateTracks={tracksToUpdate.Count}, cachedAnimTracks={_tracksCached.Count}, validRanges={_validFrameRanges.Count}, " +
				$"updateCalls={updateCalls}, record={recordMs:F2} ms, tracks=[{GetTracksSummary( tracksToUpdate )}]",
				Track );

			return success;
		}

		private static double GetElapsedMilliseconds( long startTimestamp )
		{
			return ( Stopwatch.GetTimestamp() - startTimestamp ) * 1000d / Stopwatch.Frequency;
		}

		private static string GetTracksSummary( List<FTrack> tracks )
		{
			if( tracks == null || tracks.Count == 0 )
				return "none";

			return string.Join( ", ", tracks.ConvertAll( GetTrackSummary ).ToArray() );
		}

		private static string GetTrackSummary( FTrack track )
		{
			if( track == null )
				return "null";

			int eventCount = track.Events != null ? track.Events.Count : 0;
			return string.Format( "{0}<{1}>(events={2})", track.name, track.GetType().Name, eventCount );
		}

		protected override bool ClearInternal()
		{
#if FLUX_DEBUG
			Debug.LogWarning("Destroying Preview");
#endif
			
			StopPlayback();
			
			foreach( FAnimationTrack animTrack in _tracksCached )
			{
				animTrack.Cache = null;
			}
			_tracksCached.Clear();

			return true;
		}

		/// @brief Starts playback 
		public void StartPlayback()
		{
//			if( InPlayback )
//				return;
#if FLUX_DEBUG
			Debug.Log ("Start Playback");
#endif

			if( Animator == null )
				return;

			// save transforms
			Animator.enabled = true;
			Animator.StartPlayback();

            _inPlayback = true;
        }

		/// @brief Stops playback
		public void StopPlayback()
		{
			if( Animator == null )
				return;

			// _inPlayback 只是本地标记，重建 controller / 替换缓存后可能和引擎状态不一致，
			// 用 recorderMode 兜底，避免遗留 playback 状态污染下一次录制
			if( !_inPlayback && Animator.recorderMode != AnimatorRecorderMode.Playback )
				return;
#if FLUX_DEBUG
			Debug.Log ("Stop Playback");
#endif
			Animator.StopPlayback();

            _inPlayback = false;
        }

		public override void GetPlaybackAt( float sequenceTime )
		{
#if UNITY_EDITOR
			// 没有录制数据就不要再进 playback，否则 Unity 会报
			// "Can't playback from recorder, no recorded data found"
			if( Animator == null || Animator.recorderStopTime <= Animator.recorderStartTime )
				return;
#endif
//			if( !InPlayback )
				StartPlayback();
			/// @TODO waiting for Unity crash bug to be fixed
			if( Track.Owner.gameObject.activeInHierarchy )
			{
				Animator.playbackTime = ConvertSequenceTime( sequenceTime );
				Animator.Update(0f);
			}
		}

		private float ConvertSequenceTime( float sequenceTime )
		{
			float playbackTime = 0;
			for( int i = 0; i != _validFrameRanges.Count; ++i )
			{
				float startTime = _validFrameRanges[i].Start * Track.Sequence.InverseFrameRate;
				if( startTime > sequenceTime )
				{
					// make sure we're still on the left of the preview
					playbackTime -= 0.0001f;
					break;
				}

				float endTime = _validFrameRanges[i].End * Track.Sequence.InverseFrameRate;
				if( endTime > sequenceTime )
				{
					// make sure we're on the right of the preview
					playbackTime += sequenceTime - startTime + 0.0001f;
					break;
				}
				else
				{
					playbackTime += endTime - startTime;
				}
			}

			// make sure we're just left to the end of the recorded data, because last frame will be with layer turned off
			return Mathf.Clamp( playbackTime, Animator.recorderStartTime, Animator.recorderStopTime - 0.0001f );
		}

		private List<FTrack> GetTracksToUpdate( out List<FAnimationTrack> animTracks )
		{
			List<FTrack> tracksToUpdate = new List<FTrack>();

			animTracks = new List<FAnimationTrack>();

			Transform owner = Track.Owner;

			List<FContainer> containers = Track.Sequence.Containers;

			foreach( FContainer container in containers )
			{
				List<FTimeline> timelines = container.Timelines;
				foreach( FTimeline timeline in timelines )
				{
					if( timeline.Owner != owner )
						continue;

					List<FTrack> tracks = timeline.Tracks;
					foreach( FTrack track in tracks )
					{
						if( track != null && track.enabled )
						{
                            FAnimationTrack animationTrack = track as FAnimationTrack;
							if( animationTrack )
							{
								animTracks.Add( animationTrack );
								tracksToUpdate.Add( track );
							}
							else if( track is FTransformTrack )
								tracksToUpdate.Add( track );
						}
					}
				}
			}

			return tracksToUpdate;
		}
	}
}
