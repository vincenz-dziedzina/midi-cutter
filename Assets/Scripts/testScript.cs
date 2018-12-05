using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Music.Midi;
using System.IO;
using AssemblyCSharp.Assets.Scripts;

public class testScript : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        SmfReader reader = new SmfReader();
        reader.Read(File.OpenRead("mysong2.mid"));
        MidiMusic music = reader.Music;

        Directory.CreateDirectory("midi-out");
        this.cutMusic(10000, 30000, music, "midi-out/1.mid");

        reader.Read(File.OpenRead("midi-out/1.mid"));
        MidiMusic musicCut = reader.Music;

        Debug.Log("Original file length:" + GetTimeLengthInMinutes(music));
        Debug.Log("Cut file length: " + GetTimeLengthInMinutes(musicCut));
    }

    private void cutMusic(int from, int to, MidiMusic music, string outputFilename)
    {
        FileStream stream = File.Create(outputFilename);
        SmfWriter writer = new SmfWriter(stream);
        writer.WriteHeader(music.Format, (short)music.Tracks.Count, music.DeltaTimeSpec);
        IList<MidiTrack> tracks = music.Tracks;
        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            int passedTime = 0;
            var newTrack = new MidiTrack();
            bool isFirstMessage = true;

            for (var j = 0; j < track.Messages.Count; j++)
            {
                var message = track.Messages[j];
                passedTime += message.DeltaTime;

                if (passedTime < from && message.Event.EventType != MidiEvent.NoteOn && message.Event.EventType != MidiEvent.NoteOff)
                {
                    var convertedMsg = MidiUtil.convertTimeToZero(message);
                    newTrack.AddMessage(convertedMsg);
                }
                else if (passedTime >= from && passedTime < to)
                {
                    if (isFirstMessage)
                    {
                        int newDeltaTime = passedTime - from;
                        newTrack.AddMessage(new MidiMessage(newDeltaTime, message.Event));
                        isFirstMessage = false;
                    }
                    else
                    {
                        newTrack.AddMessage(message);
                    }
                }
                else if (passedTime >= to && message.Event.EventType == MidiEvent.Meta && message.Event.MetaType == MidiMetaType.EndOfTrack)
                {
                    MidiMessage convertedMsg = MidiUtil.convertTimeToZero(message);
                    newTrack.AddMessage(convertedMsg);
                }
            }

            track = newTrack;
            // Debug.Log("Track " + (i + 1) + " Passed time:" + passedTime);
            writer.WriteTrack(track);
        }

        stream.Close();
    }

    private double GetTimeLengthInMinutes(MidiMusic midiMusic)
    {
        double timeLengthInMicroseconds = GetTimeLengthInMicroseconds(midiMusic);
        return timeLengthInMicroseconds / 1000000 / 60;
    }

    private double GetTimeLengthInSeconds(MidiMusic midiMusic)
    {
        double timeLengthInMicroseconds = GetTimeLengthInMicroseconds(midiMusic);
        return timeLengthInMicroseconds / 1000000;
    }

    private double GetTimeLengthInMicroseconds(MidiMusic midiMusic)
    {
        short division = midiMusic.DeltaTimeSpec;
        const double defaultTempo = 500000; // in microseconds per quarter-note, equals 120 beats per minute => 500000 / 1000000 * 4 * 60 = 120
        double currentTempo = defaultTempo;
        double microsecondsPerTick = 0d;

        // Debug.Log("Divisions: " + System.Convert.ToString(division, 2).PadLeft(16, '0'));
        // Debug.Log("Divisions: " + System.Convert.ToString(((ushort) ushort.MaxValue), 2));
        // Debug.Log(short.MinValue);

        if (division >> 15 == 0)
        {
            // Debug.Log("MSB is 0");
            microsecondsPerTick = currentTempo / division; //
            // Debug.Log("MicrosecondsPerTick: " + microsecondsPerTick);
        }
        else
        {
            Debug.Log("MSB is 1");
            // calculate microsecondsPerTick
            // numberOfFramesPerSecond * ticksPerFrame = ticksPerSecond
            // microSecondsPerTick = 1000000 / ticksPerSecond
        }

        double midiMusicTimeLength = 0d;

        foreach (MidiTrack midiTrack in midiMusic.Tracks)
        {
            double trackTimeLength = 0d;

            foreach (MidiMessage midiMessage in midiTrack.Messages)
            {
                trackTimeLength += midiMessage.DeltaTime * microsecondsPerTick;
                MidiEvent midiEvent = midiMessage.Event;

                if (midiEvent.EventType == MidiEvent.Meta && midiEvent.MetaType == MidiMetaType.Tempo)
                {
                    currentTempo = MidiMetaType.GetTempo(midiEvent.Data);
                    microsecondsPerTick = currentTempo / division;
                }
            }

            if (trackTimeLength > midiMusicTimeLength)
            {
                midiMusicTimeLength = trackTimeLength;
            }
        }

        return midiMusicTimeLength;
    }
    private void AddSMPTEOffsetEvent(MidiTrack track)
    {
        track.AddMessage(new MidiMessage(0, new MidiEvent(
            MidiEvent.Meta,
            MidiMetaType.SmpteOffset,
            (byte)0x05, // length: 5 bytes will follow
            new byte[] { 0x01, 0x00, 0x05, 0x00, 0x00 })));
    }

    /// Only call once per MIDI as this marks the end of the MIDI.
    /// To my knowledge it doesn't matter which track is passed.
    private void AddEndOfTrackMessage(MidiTrack track)
    {
        var evt = new MidiEvent(MidiEvent.Meta, MidiMetaType.EndOfTrack, (byte)0x00, null); // 'FF 2F 00' -> end of track
        var msg = new MidiMessage(0, evt);
        track.AddMessage(msg);
    }

    private void LogTracks(IList<MidiTrack> tracks)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            MidiTrack midiTrack = tracks[i];
            foreach (var midiMessage in midiTrack.Messages)
            {
                Debug.Log("------------------------------Start of Midi Message--------------------------------");
                if (midiMessage.Event.EventType == MidiEvent.Meta && midiMessage.Event.MetaType == MidiMetaType.EndOfTrack)
                {
                    Debug.LogWarning("End of track event");
                }
                Debug.Log("-------------------------------End of Midi Message---------------------------------");
            }
        }
    }

    private void LogMidiInformation(MidiMusic music)
    {
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

    private void LogMidiMessage(MidiMessage midiMessage)
    {
        Debug.Log("MidiMessage:");
        Debug.Log("Delta time: " + midiMessage.DeltaTime);
        LogMidiEvent(midiMessage.Event);
    }

    private void LogMidiEvent(MidiEvent midiEvent)
    {
        Debug.Log("Event:");
        Debug.Log("Event type: " + System.String.Format("{0:X}", midiEvent.EventType));
        Debug.Log("Event Meta type: " + System.String.Format("{0:X}", midiEvent.MetaType));

        if (midiEvent.Data != null)
        {
            Debug.Log("Data: " + System.BitConverter.ToString(midiEvent.Data));
        }
        else
        {
            Debug.Log("Data: None");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
