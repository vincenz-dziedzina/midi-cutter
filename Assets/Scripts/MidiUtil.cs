using System;
using Commons.Music.Midi;

namespace AssemblyCSharp.Assets.Scripts
{
    public static class MidiUtil
    {

        public const int MICROSECONDS_PER_SECOND = 1000000;
        public const int SECONDS_PER_MINUTE = 60;

        public static MidiMessage convertTimeToZero(MidiMessage message)
        {
            return new MidiMessage(0, message.Event);
        }

    }
}
