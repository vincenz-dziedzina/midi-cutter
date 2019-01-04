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
        cutter.readFile("mysong2.mid");
        Debug.Log("Duration (seconds): " + cutter.GetDurationInSeconds());
        Debug.Log("Track count: " + cutter.getTrackCount());
        IList<int> trackNums = new List<int>() { 1, 2, 3 };
        IList<MidiCutter.CutSpecification> cutSpecs = cutter.getTracks(trackNums).Select(t => new MidiCutter.CutSpecification(t, 25000, 35000)).ToList();
        IList<MidiTrack> tracks = cutter.cutTracksByCutSpecification(cutSpecs);
        cutter.writeTracksToFile(tracks, "new_out.mid");
    }

}