using UnityEngine;
using midi_cutter.Assets.Scripts;
public class MidiCutExample : MonoBehaviour
{

    void Start() 
    {
        MidiCutter cutter = new MidiCutter();
        cutter.readFile("mysong2.mid");
        Debug.Log("Duration (seconds): " + cutter.GetDurationInSeconds());
        cutter.cut(25000, 35000, "new_out.mid");
    }

}