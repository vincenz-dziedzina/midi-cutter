using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Music.Midi;
using System.IO;

public class testScript : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        SmfReader reader = new SmfReader();
        reader.Read(File.OpenRead("mysong2.mid"));
        var music = reader.Music;
        var tracks = music.Tracks;
        var stream = File.Create("newsong.mid");
        SmfWriter writer = new SmfWriter(stream);
        writer.WriteHeader(music.Format, (short)music.Tracks.Count, music.DeltaTimeSpec);
        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            int passedTime = 0;
            var newTrack = new MidiTrack();
            for (var j = 0; j < track.Messages.Count; j++)
            {
                var message = track.Messages[j];
                passedTime += message.DeltaTime;

                if (passedTime < 20000)
                {
                    newTrack.AddMessage(message);
                }
            }
            track = newTrack;
            Debug.Log("Track " + tracks.IndexOf(track) + " Passed time:" + passedTime);
            writer.WriteTrack(track);
        }
        this.AddEndOfTrackMessage(tracks[0]);
        Debug.Log("DELTA TIME: " + music.DeltaTimeSpec);
        Debug.Log("DONE");
    }

    /// Only call once per MIDI as this marks the end of the MIDI.
    /// To my knowledge it doesn't matter which track is passed.
    private void AddEndOfTrackMessage(MidiTrack track)
    {
        var evt = new MidiEvent(12032); // 'FF 2F 00' -> end of track
        var msg = new MidiMessage(0, evt);
        track.AddMessage(msg);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
