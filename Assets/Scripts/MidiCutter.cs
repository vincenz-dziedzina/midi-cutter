using System.IO;
using System.Collections.Generic;
using AssemblyCSharp.Assets.Scripts;
using Commons.Music.Midi;

namespace midi_cutter.Assets.Scripts
{

    public class MidiCutter
    {
        public string outputDirName;
        private MidiMusic music;

        // constructor
        public MidiCutter(string outputDir = "midi-out") 
        {
            this.outputDirName = outputDir;
            this.createOutputDirectory();
        }

        // destructor
        ~MidiCutter() {

        }

        /// <summary>
        /// Reads the given file with the given fileName. Expects a MIDI standard file. Otherwise an error will be trown.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public void readFile(string filePath) {
            FileStream fileStream = File.OpenRead(filePath);
            SmfReader reader = new SmfReader();
            reader.Read(fileStream);
            this.music = reader.Music;
            fileStream.Close();
        }

        /// <summary>
        /// Creates the output directory and ensures it is empty.
        /// </summary>
        private void createOutputDirectory() 
        {
            DirectoryInfo dir = Directory.CreateDirectory(this.outputDirName);
            foreach (FileInfo file in dir.GetFiles()) {
                file.Delete();
            }
            foreach (DirectoryInfo d in dir.GetDirectories()) {
                d.Delete();
            }
        }
        
        /// <summary>
        /// Creates a file with the passed name within the output-directory.
        /// </summary>
        /// <param name="name">The filename including the extension.</param>
        /// <returns>Returns the FileStream.</returns>
        private FileStream createFile(string name) 
        {
            string fullPath = Path.Combine(this.outputDirName, name);
            return File.Create(fullPath);
        }

        /// <summary>
        /// Cuts MIDI between "from" and "to" and writes the result to a new MIDI file.
        /// </summary>
        /// <param name="from">Start of the cut, inclusive.</param>
        /// <param name="to">End of the cut, exclusive</param>
        /// <param name="outputFileName">Name of resulting file written to the output-directory.</param>
        private void cutMusic(int from, int to, string outputFileName)
        {
            FileStream stream = this.createFile(outputFileName);
            SmfWriter writer = new SmfWriter(stream);
            writer.WriteHeader(music.Format, (short) music.Tracks.Count, music.DeltaTimeSpec);
            IList<MidiTrack> tracks = music.Tracks;
            foreach (MidiTrack track in tracks)
            {
                int passedTime = 0;
                MidiTrack resultTrack = new MidiTrack();
                bool isFirstMessage = true;

                foreach (MidiMessage msg in track.Messages)
                {
                    passedTime += msg.DeltaTime;

                    // messages before 'from' or messages after 'to': 
                    // Keep each message that isn't playing a note but give it a delta time of zero.
                    if (passedTime < from && msg.Event.EventType != MidiEvent.NoteOn && msg.Event.EventType != MidiEvent.NoteOff ||
                        passedTime >= to && msg.Event.EventType == MidiEvent.Meta && msg.Event.MetaType == MidiMetaType.EndOfTrack)
                    {
                        MidiMessage convertedMsg = MidiUtil.convertTimeToZero(msg);
                        resultTrack.AddMessage(convertedMsg);
                    }
                    // messages within 'from' and 'to': 
                    // Keep these, just make sure the first message has the right time offset.
                    else if (passedTime >= from && passedTime < to)
                    {
                        if (isFirstMessage)
                        {
                            resultTrack.AddMessage(new MidiMessage(passedTime - from, msg.Event));
                            isFirstMessage = false;
                        }
                        else
                        {
                            resultTrack.AddMessage(msg);
                        }
                    }
                }
                writer.WriteTrack(resultTrack);
            }
            stream.Close();
        }

        /// <summary>
        /// Get the MIDI's duration in minutes.
        /// </summary>
        public double GetDurationInMinutes()
        {
            return this.GetDurationInSeconds() / 60;
        }

        /// <summary>
        /// Get the MIDI's duration in seconds.
        /// </summary>
        public double GetDurationInSeconds()
        {
            return GetDurationInMicroseconds() / 1000000;
        }

        /// <summary>
        /// Get the MIDI's duration in microseconds.
        /// </summary>
        public double GetDurationInMicroseconds()
        {
            ushort division = (ushort) this.music.DeltaTimeSpec;
            const double defaultTempo = 500000; // in microseconds per quarter-note, equals 120 beats per minute => 500000 / 1000000 * 4 * 60 = 120
            double currentTempo = defaultTempo;
            double microsecondsPerTick = 0d;

            if (division >> 15 == 0)
            {
                microsecondsPerTick = currentTempo / division;
            }
            else
            {   
                byte bitmask = 0xFF; // 1111_1111
                byte bits = (byte) ((division >> 8) & bitmask);
                byte negatedFramesPerSecond = (byte) ~bits;
                byte framesPerSecond = (byte) (negatedFramesPerSecond + 1);
                byte ticksPerFrame = (byte) (division & bitmask);
                double ticksPerSecond = ticksPerFrame * framesPerSecond;
                microsecondsPerTick = 1000000 / ticksPerSecond;
            }
            double midiMusicTimeLength = 0d;

            foreach (MidiTrack midiTrack in this.music.Tracks)
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
    }
}