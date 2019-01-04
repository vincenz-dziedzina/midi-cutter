using System.IO;
using System.Collections.Generic;
using AssemblyCSharp.Assets.Scripts;
using Commons.Music.Midi;
using System.Linq;

namespace midi_cutter.Assets.Scripts
{

    public struct CutSpecification
    {
        public MidiTrack midiTrack;
        public int fromTick;
        public int toTick;

        public CutSpecification(MidiTrack track, int fromTick, int toTick)
        {
            this.midiTrack = track;
            this.fromTick = fromTick;
            this.toTick = toTick;
        }
    }

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
        ~MidiCutter()
        {

        }

        /// <summary>
        /// Reads the given file with the given fileName. Expects a MIDI standard file. Otherwise an error will be trown.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public void readFile(string filePath)
        {
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
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
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
        /// Returns the tracks with the given track numbers. Always includes track "0" as that one includes meta-information other tracks rely on.
        /// </summary>
        /// <param name="trackNums">The track numbers</param>
        /// <returns></returns>
        public IList<MidiTrack> getTracks(IList<int> trackNums)
        {
            if (!trackNums.Contains(0))
            {
                trackNums.Add(0);
            }
            return trackNums.Select(trackNum => this.music.Tracks[trackNum]).ToList();
        }

        /// <summary>
        /// Writes tracks to specified file
        /// </summary>
        /// <param name="outputFileName">Name of the destination file</param>
        /// <param name="tracks">Tracks to write</param>
        public void writeTracksToFile(IList<MidiTrack> tracks, string outputFileName)
        {
            foreach (MidiTrack midiTrack in tracks)
            {
                FileStream stream = this.createFile(outputFileName);
                SmfWriter writer = new SmfWriter(stream);

                writer.WriteHeader(music.Format, (short)music.Tracks.Count, music.DeltaTimeSpec);
                foreach (MidiTrack track in tracks)
                {
                    writer.WriteTrack(track);
                }

                stream.Close();
            }
        }

        /// <summary>
        /// Cuts MIDI between "from" and "to" and writes the result to a new MIDI file.
        /// </summary>
        /// <param name="fromTick">Start of the cut in ticks, inclusive.</param>
        /// <param name="toTick">End of the cut in ticks, exclusive</param>
        /// <param name="outputFileName">Name of resulting file written to the output-directory.</param>
        private void cutMidiByTicks(int fromTick, int toTick, string outputFileName)
        {
            IList<MidiTrack> cutMidiTracks = new List<MidiTrack>();

            foreach (MidiTrack track in music.Tracks)
            {
                MidiTrack resultTrack = this.cutTrackByTicks(track, fromTick, toTick);
                cutMidiTracks.Add(resultTrack);
            }

            writeTracksToFile(cutMidiTracks, outputFileName);
        }

        /// <summary>
        /// Cuts track by "from" and "to" in ticks
        /// </summary>
        /// <param name="track"></param>
        /// <param name="fromTick"></param>
        /// <param name="toTick"></param>
        /// <returns></returns>
        private MidiTrack cutTrackByTicks(MidiTrack track, int fromTick, int toTick)
        {
            int passedTicks = 0;
            MidiTrack resultTrack = new MidiTrack();
            bool isFirstMessage = true;

            foreach (MidiMessage msg in track.Messages)
            {
                passedTicks += msg.DeltaTime;

                // messages before 'from' or messages after 'to': 
                // Keep each message that isn't playing a note but give it a delta time of zero.
                if (passedTicks < fromTick && msg.Event.EventType != MidiEvent.NoteOn && msg.Event.EventType != MidiEvent.NoteOff ||
                    passedTicks >= toTick && msg.Event.EventType == MidiEvent.Meta && msg.Event.MetaType == MidiMetaType.EndOfTrack)
                {
                    MidiMessage convertedMsg = MidiUtil.convertTimeToZero(msg);
                    resultTrack.AddMessage(convertedMsg);
                }
                // messages within 'from' and 'to': 
                // Keep these, just make sure the first message has the right time offset.
                else if (passedTicks >= fromTick && passedTicks < toTick)
                {
                    if (isFirstMessage)
                    {
                        resultTrack.AddMessage(new MidiMessage(passedTicks - fromTick, msg.Event));
                        isFirstMessage = false;
                    }
                    else
                    {
                        resultTrack.AddMessage(msg);
                    }
                }
            }

            return resultTrack;
        }

        public IList<MidiTrack> cutTracksByCutSpecification(IList<CutSpecification> cutSpecs)
        {
            IList<MidiTrack> cutMidiTracks = new List<MidiTrack>();

            foreach (CutSpecification cutSpec in cutSpecs)
            {
                MidiTrack track = cutTrackByTicks(cutSpec.midiTrack, cutSpec.fromTick, cutSpec.toTick);
                cutMidiTracks.Add(track);
            }

            return cutMidiTracks;
        }

        /// <summary>
        /// Returns the exact tick at the time within the MIDI.
        /// </summary>
        /// <param name="time">The time in microseconds.</param>
        /// <returns></returns>
        private int microsecondsToTicks(double time)
        {
            // return the maximum tick of all tracks, because otherwise we won't correctly target every track's tick if their lengths vary
            double microsecondsPerTick = getMicrosecondsPerTick();
            IList<int> ticksPerTrack = new List<int>();
            foreach (MidiTrack track in music.Tracks)
            {
                ticksPerTrack.Add(microsecondsToTicks(track, time, microsecondsPerTick));
            }
            return ticksPerTrack.Max();
        }

        /// <summary>
        /// Returns the exact tick at the time within a MIDI track.
        /// </summary>
        /// <param name="track">The MIDI track</param>
        /// <param name="time">The time in microseconds.</param>
        /// <param name="microsecondsPerTick">The microSeconds per tick of the MIDI file.</param>
        /// <returns></returns>
        private int microsecondsToTicks(MidiTrack track, double time, double microsecondsPerTick)
        {
            ushort division = (ushort)this.music.DeltaTimeSpec;
            double currentTempo = MidiUtil.MIDI_DEFAULT_TEMPO;
            int passedTicks = 0;
            double passedMicroSeconds = 0d;

            foreach (MidiMessage midiMessage in track.Messages)
            {
                passedMicroSeconds += midiMessage.DeltaTime * microsecondsPerTick;
                passedTicks += midiMessage.DeltaTime;
                MidiEvent midiEvent = midiMessage.Event;

                if (passedMicroSeconds > time)
                {
                    // if we overstepped the set time get the time between this message and the previous one
                    double prevMsgTime = passedMicroSeconds - midiMessage.DeltaTime * microsecondsPerTick;
                    return (int)((time - prevMsgTime) / microsecondsPerTick);
                }

                if (midiEvent.EventType == MidiEvent.Meta && midiEvent.MetaType == MidiMetaType.Tempo)
                {
                    currentTempo = MidiMetaType.GetTempo(midiEvent.Data);
                    microsecondsPerTick = currentTempo / division;
                }
            }
            return passedTicks;
        }

        /// <summary>
        /// Returns the number of tracks excluding the meta track.
        /// </summary>
        /// <returns></returns>
        public int getTrackCount()
        {
            return this.music.Tracks.Count() - 1;
        }

        /// <summary>
        /// Get the microSeconds per tick of the MIDI file. This may change throughout the song due to tempo events.
        /// </summary>
        /// <returns>The microseconds per tick.</returns>
        private double getMicrosecondsPerTick()
        {
            ushort division = (ushort)this.music.DeltaTimeSpec;

            if (division >> 15 == 0)
            {
                return MidiUtil.MIDI_DEFAULT_TEMPO / division;
            }
            else
            {
                byte bitmask = 0xFF; // 1111_1111
                byte bits = (byte)((division >> 8) & bitmask);
                byte negatedFramesPerSecond = (byte)~bits;
                byte framesPerSecond = (byte)(negatedFramesPerSecond + 1);
                byte ticksPerFrame = (byte)(division & bitmask);
                double ticksPerSecond = ticksPerFrame * framesPerSecond;
                return 1000000 / ticksPerSecond;
            }

        }

        /// <summary>
        /// Get the MIDI's duration in minutes.
        /// </summary>
        public double GetDurationInMinutes()
        {
            return this.GetDurationInSeconds() / MidiUtil.SECONDS_PER_MINUTE;
        }

        /// <summary>
        /// Get the MIDI's duration in seconds.
        /// </summary>
        public double GetDurationInSeconds()
        {
            return GetDurationInMicroseconds() / MidiUtil.MICROSECONDS_PER_SECOND;
        }

        /// <summary>
        /// Get the MIDI's duration in microseconds.
        /// </summary>
        public double GetDurationInMicroseconds()
        {
            double microsecondsPerTick = this.getMicrosecondsPerTick();
            ushort division = (ushort)this.music.DeltaTimeSpec;
            double currentTempo = MidiUtil.MIDI_DEFAULT_TEMPO;
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