//#define FLUX_DEBUG
using UnityEngine;
using System.Collections.Generic;

namespace Flux
{
	/// @brief Cache for FAnimationTrack
	public class FAnimationTrackCache : FTrackCache {
		
		private Animator _animator = null;
		public Animator Animator
		{
			get {
				if( _animator == null )
					_animator = Track.Owner.GetComponent<Animator>();
				return _animator;
			}
		}

        // @note, can't just check Animator.playbackTime >= 0 because unity prints a warning if it's not in playback :(
        private bool _inPlayback = false;
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
			if( speed != 1 )
				sequence.Speed = 1f;
			
			Animator.speed = 1f;
			
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

			Animator.StartRecording( -1 );

			bool success = Animator.recorderMode == AnimatorRecorderMode.Record;

			if( success )
			{
				Animator.enabled = false;

				float delta = 1f / sequence.FrameRate;
				int frame = 0;
				
				while( frame <= sequence.Length )
				{
					bool wasEnabled = Animator.enabled;

					foreach( FTrack track in tracksToUpdate )
						track.UpdateEvents( frame, frame * delta );

					if( wasEnabled )
					{
						currentFrameRange.End += 1;
						if( Animator.enabled )
						{
							Animator.Update( delta );
						}
						else
						{
							Animator.enabled = true;
							Animator.Update( delta );
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

			return success;
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

			// save transforms
			Animator.enabled = true;
			Animator.StartPlayback();

            _inPlayback = true;
        }

		/// @brief Stops playback
		public void StopPlayback()
		{
			if( !InPlayback )
				return;
#if FLUX_DEBUG
			Debug.Log ("Stop Playback");
#endif
			Animator.StopPlayback();

            _inPlayback = false;
        }

		public override void GetPlaybackAt( float sequenceTime )
		{
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
