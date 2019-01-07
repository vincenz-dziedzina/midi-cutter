using UnityEngine;
using System.Collections.Generic;
using Commons.Music.Midi;
using System.Linq;
using midi_cutter.Assets.Scripts;
public class MidiCutExample : MonoBehaviour
{

    void Start()
    {
        MidiCutter cutter = new MidiCutter();
        cutter.readFile("MidiExamples/shorttrack.mid");
        Debug.Log("Duration (seconds): " + cutter.GetDurationInSeconds());
        Debug.Log("Track duration (ticks): " + cutter.GetDurationInTicks());
        Debug.Log("Track count: " + cutter.getTrackCount());
        IList<int> trackNums = new List<int>() { 1 };
        
        IList<CutSpecification> cutSpecs = cutter.getTracks(trackNums).Select(t => new CutSpecification(t, 0, 200)).ToList();
        IList<MidiTrack> tracks = cutter.cutTracksByCutSpecification(cutSpecs);
        cutter.writeTracksToFile(tracks, "new_out.mid");

        // cutSpecs = cutter.getTracks(trackNums).Select(t => new CutSpecification(t, 500, 1300)).ToList();
        // tracks = cutter.cutTracksByCutSpecification(cutSpecs);
        // cutter.writeTracksToFile(tracks, "new_out1.mid");
    }

}