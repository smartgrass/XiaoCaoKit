using UnityEngine;
using System.Collections.Generic;
using System;

namespace Flux
{
	public class FTransformTrack : FTrack {
		// store transform snapshots in <sequence, <transform, snapshot>>
		private static Dictionary<FSequence, Dictionary<Transform, TransformSnapshot>> _snapshots = new Dictionary<FSequence, Dictionary<Transform, TransformSnapshot>>();

		public static TransformSnapshot GetSnapshot( FSequence sequence, Transform transform )
		{
			if( transform == null )
				return null;
			
			Dictionary<Transform, TransformSnapshot> sequenceSnapshots = null;
			if( !_snapshots.TryGetValue(sequence, out sequenceSnapshots) )
			{
				sequenceSnapshots = new Dictionary<Transform, TransformSnapshot>();
				_snapshots.Add( sequence, sequenceSnapshots );
			}

			TransformSnapshot result = null;
			if( !sequenceSnapshots.TryGetValue(transform, out result) )
			{
				result = new TransformSnapshot(transform);
				sequenceSnapshots.Add( transform, result );
			}
			return result;
		}

		protected TransformSnapshot _snapshot = null;
		public TransformSnapshot Snapshot { get { return _snapshot; } }

		public override void Init()
		{
			base.Init();

			_snapshot = GetSnapshot(Sequence, Owner);
		}

		public override void Stop()
		{
			base.Stop();

			if( _snapshot != null )
				_snapshot.Restore();
		}

        public void ClearSnapshot()
        {
            foreach (var dic in _snapshots.Values)
            {
                foreach (var item in dic.Keys)
                {
                    if (item != null)
                        Debug.Log("FLog  ClearSnapshot" + item.name);
                    else
                        Debug.Log($"FLog clear null");
                }
                dic.Clear();
            }
            _snapshots.Clear();
        }
    }

	public class TransformSnapshot
	{
		public Transform Transform { get; private set; }
        public RectTransform RectTransform { get; private set; }

		public Transform Parent { get; private set; }
		public Vector3 LocalPosition { get; private set; } // used for anchor position in RectTransform
		public Quaternion LocalRotation { get; private set; }
		public Vector3 LocalScale { get; private set; }

        // for RectTransform
        public Vector2 AnchorMin { get; private set; }
	    public Vector2 AnchorMax { get; private set; }
	    public Vector2 Pivot { get; private set; }

        public TransformSnapshot[] ChildrenSnapshots = null;

		public TransformSnapshot( Transform transform, bool recursive = false )
		{
			Transform = transform;

            RectTransform = Transform as RectTransform;
		    
			Parent = Transform.parent;

		    if (RectTransform != null)
		    {
		        LocalPosition = RectTransform.anchoredPosition3D;
		        AnchorMin = RectTransform.anchorMin;
		        AnchorMax = RectTransform.anchorMax;
		        Pivot = RectTransform.pivot;
            }
		    else
		    {
		        LocalPosition = Transform.localPosition;
            }

			LocalRotation = Transform.localRotation;
			LocalScale = Transform.localScale;

			if( recursive )
			{
				TakeChildSnapshots();
			}
		}

		public void TakeChildSnapshots()
		{
			if( ChildrenSnapshots != null )
				return;
			ChildrenSnapshots = new TransformSnapshot[Transform.childCount];
			for( int i = 0; i != ChildrenSnapshots.Length; ++i )
			{
				ChildrenSnapshots[i] = new TransformSnapshot( Transform.GetChild(i), true );
			}
		}

		public void Restore()
		{
			if( Parent != Transform.parent )
				Transform.SetParent( Parent );

		    if (RectTransform != null)
		    {
		        RectTransform.anchoredPosition3D = LocalPosition;
		        RectTransform.anchorMin = AnchorMin;
		        RectTransform.anchorMax = AnchorMax;
		        RectTransform.pivot = Pivot;
		    }
		    else
		    {
		        Transform.localPosition = LocalPosition;
            }

		    Transform.localRotation = LocalRotation;
		    Transform.localScale = LocalScale;

		    if( ChildrenSnapshots != null )
			{
				for( int i = 0; i != ChildrenSnapshots.Length; ++i )
					ChildrenSnapshots[i].Restore();
			}
		}
	}
}
