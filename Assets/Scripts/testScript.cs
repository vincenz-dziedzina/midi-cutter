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

        LogMidiInformation(music);

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            int passedTime = 0;
            var newTrack = new MidiTrack();

            if(i == 0) {
                AddSMPTEOffsetEvent(newTrack);
            }

            for (var j = 0; j < track.Messages.Count; j++)
            {
                var midiMessage = track.Messages[j];

                passedTime += midiMessage.DeltaTime;

                if(midiMessage.Event.EventType == MidiEvent.Meta || passedTime < 20000) {
                    newTrack.AddMessage(midiMessage);
                }
            }

            track = newTrack;
            Debug.Log("Track " + (i + 1) + " Passed time:" + passedTime);
            writer.WriteTrack(track);
        }
        AddEndOfTrackMessage(tracks[0]);
        Debug.Log("DELTA TIME: " + music.DeltaTimeSpec);
        Debug.Log("DONE");
    }

    private void AddSMPTEOffsetEvent(MidiTrack track) {
        track.AddMessage(new MidiMessage(0, new MidiEvent(
            MidiEvent.Meta, 
            MidiMetaType.SmpteOffset, 
            (byte) 0x05, // length: 5 bytes will follow
            new byte[] { 0x01, 0x00, 0x05, 0x00, 0x00 })));
    }

    /// Only call once per MIDI as this marks the end of the MIDI.
    /// To my knowledge it doesn't matter which track is passed.
    private void AddEndOfTrackMessage(MidiTrack track)
    {
        var evt = new MidiEvent(MidiEvent.Meta, MidiMetaType.EndOfTrack, (byte) 0x00, null); // 'FF 2F 00' -> end of track
        var msg = new MidiMessage(0, evt);
        track.AddMessage(msg);
    }

    private void LogTracks(IList<MidiTrack> tracks) {
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

    private void LogMidiInformation(MidiMusic music) {
        Debug.Log("Divisions: " + System.Convert.ToString(music.DeltaTimeSpec, 2));
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

    private void LogMidiMessage(MidiMessage midiMessage) {
        Debug.Log("MidiMessage:");
        Debug.Log("MidiMessage: " + midiMessage);
        Debug.Log("Delta time: " + midiMessage.DeltaTime);
        LogMidiMessage(midiMessage.Event);
    }

    private void LogMidiMessage(MidiEvent midiEvent) {
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
