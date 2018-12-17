using System;
using Commons.Music.Midi;

namespace AssemblyCSharp.Assets.Scripts
{
    public static class MidiUtil
    {

        public const int MICROSECONDS_PER_SECOND = 1000000;
        public const int SECONDS_PER_MINUTE = 60;

        /// <summary>
        /// In microseconds per quarter-note, equals 120 beats per minute 
        /// 
        /// 500000 / 1000000 * 4 * 60 = 120
        /// </summary>
        public const int MIDI_DEFAULT_TEMPO = 500000;

        public static MidiMessage convertTimeToZero(MidiMessage message)
        {
            return new MidiMessage(0, message.Event);
        }

    }
}
