using System;
using Commons.Music.Midi;

namespace AssemblyCSharp.Assets.Scripts
{
    public static class MidiUtil
    {

        public static MidiMessage convertTimeToZero(MidiMessage message)
        {
            return new MidiMessage(0, message.Event);
        }

    }
}
