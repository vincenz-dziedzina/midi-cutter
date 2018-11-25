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
        reader.Read(File.OpenRead("mj.mid"));

        MidiMusic music = reader.Music;
        IList<MidiTrack> tracks = music.Tracks;
        FileStream stream = File.Create("newsong.mid");

        SmfWriter writer = new SmfWriter(stream);
        writer.WriteHeader(music.Format, (short)music.Tracks.Count, music.DeltaTimeSpec);

        logMidiInformation(music);

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            int passedTime = 0;
            var newTrack = new MidiTrack();
            for (var j = 0; j < track.Messages.Count; j++)
            {
                var midiMessage = track.Messages[j];

                passedTime += midiMessage.DeltaTime;

                if(midiMessage.Event.EventType == MidiEvent.Meta) {
                    newTrack.AddMessage(midiMessage);
                } else if(passedTime < 2000) {
                    newTrack.AddMessage(midiMessage);
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

    private void logTracks(IList<MidiTrack> tracks) {
        for (int i = 0; i < tracks.Count; i++)
        {
            MidiTrack midiTrack = tracks[i];
            foreach (var midiMessage in midiTrack.Messages)
            {
                Debug.Log("------------------------------Start of Midi Message--------------------------------");
                if(midiMessage.Event.EventType == MidiEvent.Meta && midiMessage.Event.MetaType == MidiMetaType.EndOfTrack) {
                    Debug.LogWarning("End of track event");
                }
                Debug.Log("-------------------------------End of Midi Message---------------------------------");
            }
        }
    }

    private void logMidiInformation(MidiMusic music) {
        Debug.Log("Divisions: " + music.DeltaTimeSpec);
        Debug.Log("Format: " + music.Format);
        Debug.Log("Number of tracks: " + music.Tracks.Count);
        try
        {
            Debug.Log("Time in milliseconds" + music.GetTotalPlayTimeMilliseconds());
        }
        catch (System.NotSupportedException)
        {
            Debug.Log("Library does not support calculating time of midi files with any format other than 0.");
        }
       
    }

    private void logMidiMessage(MidiMessage midiMessage) {
        Debug.Log("MidiMessage:");
        Debug.Log("MidiMessage: " + midiMessage);
        Debug.Log("Delta time: " + midiMessage.DeltaTime);
        logMidiEvent(midiMessage.Event);
    }

    private void logMidiEvent(MidiEvent midiEvent) {
        Debug.Log("Event:");
        Debug.Log("Event type: " + System.String.Format("{0:X}", midiEvent.EventType));
        Debug.Log("Event Meta type: " + System.String.Format("{0:X}", midiEvent.MetaType));

        if(midiEvent.Data != null) {
            Debug.Log("Data: " + System.BitConverter.ToString(midiEvent.Data));
        } else {
            Debug.Log("Data: None");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
