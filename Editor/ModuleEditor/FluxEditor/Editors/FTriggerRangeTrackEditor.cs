
using Flux;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XiaoCao;
namespace FluxEditor
{
    [FEditor(typeof(FTriggerRangeTrack))]
    public class FTriggerRangeTrackEditor : FTrackEditor
    {
        //SceneªÊ÷∆
        TriggerDrawer drawer;

        public override void Init(FObject obj, FEditor owner)
        {
            base.Init(obj, owner);
            drawer = GameObject.FindObjectOfType<TriggerDrawer>();
            if(drawer == null)
            {
                drawer = new GameObject("drawTrigger").AddComponent<TriggerDrawer>();
            }
        }


        protected override void OnEnable()
        {
            base.OnEnable();
        }


        public override void OnStop()
        {
            base.OnStop();
            drawer.Remove(Track);
        }
        protected override void OnDestroy()
        {
            drawer.Clear();
            base.OnDestroy();
        }

        public override void UpdateEventsEditor(int frame, float time)
        {
            drawer.frame = frame;
            base.UpdateEventsEditor(frame, time);
            FEvent[] evts = new FEvent[2];
            int numEvents = Track.GetEventsAt(frame, evts);
            if (numEvents == 0)
            {
                drawer.Remove(Track);
                return;
            }
            drawer.Regist(Track as FTriggerRangeTrack);
            //Debug.Log($"--- draw {frame}");

            //foreach (var item in Track.Events)
            //{
            //    var ev = (FTriggerRangeEvent)item;
            //    if (item.FrameRange.Contains(frame))
            //    {

            //        drawCube.Add(Track ,ev.cubeRange, ev.Owner);
            //    }
            //    else
            //    {
            //        drawCube.Remove(Track,ev.cubeRange);
            //    }
            //}
        }
    }
}
