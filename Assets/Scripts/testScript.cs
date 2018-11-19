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
        //var music = MidiMusic.Read(System.IO.File.OpenRead("mysong.mid"));
        //var player = new MidiPlayer(music, output);
        //player.EventReceived += (MidiEvent e) => {
        //    if (e.EventType == MidiEvent.Program)
        //        Debug.Log($"Program changed: Channel:{e.Channel} Instrument:{e.Msb}");
        //};
        //player.PlayAsync();
        SmfReader reader = new SmfReader();
        reader.Read(System.IO.File.OpenRead("mysong.mid"));
        var music = reader.Music;
        var tracks = music.Tracks;
        var splitMusic = SmfTrackSplitter.Split(tracks[0].Messages, music.DeltaTimeSpec);
        byte[] data = splitMusic.Tracks[0].Messages[0].Event.Data;
        var stream = File.Create("newsong.mid");
        SmfWriter writer = new SmfWriter(stream);
        writer.WriteHeader(music.Format, /*(short)music.Tracks.Count*/2, music.DeltaTimeSpec);
        writer.WriteTrack(tracks[0]);
        writer.WriteTrack(tracks[1]);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
