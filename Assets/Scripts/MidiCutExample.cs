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
        IList<int> trackNums = new List<int>() {1, 2, 3};
        IList<MidiTrack> tracks = cutter.getTracks(trackNums);
        IList<MidiCutter.CutSpecification> cutSpecs = tracks.Select(t => new MidiCutter.CutSpecification(t, 25000, 35000)).ToList();
        IList<MidiTrack> cutTracks = cutter.cutTracksByCutSpecification(cutSpecs);
        cutter.writeTracksToFile(cutTracks, "new_out.mid");
    }

}