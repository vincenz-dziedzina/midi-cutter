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

                if (passedTime < 20000 || message.Event.EventType != MidiEvent.NoteOn && message.Event.EventType != MidiEvent.NoteOff)
                {
                    newTrack.AddMessage(message);
                }
            }
            track = newTrack;
            Debug.Log("Track " + tracks.IndexOf(track) + " Passed time:" + passedTime);
            writer.WriteTrack(track);
        }
        Debug.Log("DELTA TIME: " + music.DeltaTimeSpec);
        Debug.Log("DONE");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
