using Flux;
using FluxEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluxEditor
{
    ///<see cref="FMsgEvent"/>
    [FEditor(typeof(FMessageTrack))]
    public class FMessageTrackEditor : FTrackEditor
    {

        /*
        public override void UpdateEventsEditor(int frame, float time)
        {
            base.UpdateEventsEditor(frame, time);

            FEvent[] evts = new FEvent[2];

            int numEvents = Track.GetEventsAt(frame, evts);
            if (numEvents == 0)
                return;

            var _event0 = evts[0];
            if (!_event0.enabled)
                return;

            //if (Track.GetEventType() == typeof(FMessageTrack))
            //{


            //}

        }*/

    }




    public class FMessageTrack : FTrack
    {

    }

}
