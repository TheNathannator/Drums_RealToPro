using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Drums_RealToPro
{
    partial class Program
    {
        /// <summary>
        /// The folder to edit the charts of.
        /// </summary>
        static string FolderPath = null;
        static ReadingSettings MidiReadSettings = new ReadingSettings
        {
            ZeroLengthDataPolicy = ZeroLengthDataPolicy.ReadAsEmptyObject,
            TextEncoding = System.Text.Encoding.UTF8,
            NoHeaderChunkPolicy = NoHeaderChunkPolicy.Ignore,
            NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
            InvalidSystemCommonEventParameterValuePolicy = InvalidSystemCommonEventParameterValuePolicy.SnapToLimits,
            InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
            InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.SnapToLimits,
            UnknownChannelEventPolicy = UnknownChannelEventPolicy.Abort,
            UnknownFileFormatPolicy = UnknownFileFormatPolicy.Abort,
            InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
            SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOff,
            MissedEndOfTrackPolicy = MissedEndOfTrackPolicy.Ignore,
            UnknownChunkIdPolicy = UnknownChunkIdPolicy.ReadAsUnknownChunk,
            ExtraTrackChunkPolicy = ExtraTrackChunkPolicy.Read,
            UnexpectedTrackChunksCountPolicy = UnexpectedTrackChunksCountPolicy.Ignore,
            EndOfTrackStoringPolicy = EndOfTrackStoringPolicy.Omit
        };
        static EnumerationOptions FileEnumOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false
        };
        static WritingSettings midiWriteSettings = new WritingSettings
        {
            TextEncoding = System.Text.Encoding.UTF8,
        };

        static void Main(string[] args)
        {
            // Register unhandled exception event
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            // Get the folder path
            if (args.Length < 1)
            {
                if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Songs")))
                {
                    Console.WriteLine("Enter the path to convert charts in, or leave blank to use the Songs folder in the program's directory.");
                }
                else
                {
                    Console.WriteLine("Enter the path to convert charts in, or leave blank to create and use a Songs folder in the program's directory.");
                    Console.WriteLine("(You will be given a moment to move the songs in.)");
                }

                do
                {
                    string inputPath = Console.ReadLine().Replace("\"", "");
                    if (String.IsNullOrEmpty(inputPath))
                    {
                        FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Songs");
                        if (!Directory.Exists(FolderPath))
                        {
                            Directory.CreateDirectory(FolderPath);
                            Console.WriteLine("A Songs folder has been created. Place the songs to be converted into that folder, then press any key to continue.");
                            Console.ReadKey(intercept: true);
                        }
                        else
                        {
                            Console.WriteLine("Place the songs to be converted into the Songs folder, then press any key to continue.");
                            Console.ReadKey(intercept: true);
                        }
                    }
                    else
                    {
                        if (Directory.Exists(inputPath))
                        {
                            FolderPath = inputPath;
                        }
                        else
                        {
                            Console.WriteLine("The specified path does not exist. Please make sure that it exists and is spelled correctly.");
                        }
                    }
                }
                while (FolderPath == null);
            }
            else
            {
                if (Directory.Exists(args[0]))
                {
                    FolderPath = args[0];
                }
            }

            // Overbearing safety lol
            if (FolderPath == null)
            {
                Console.WriteLine("The specified path could not be used for an unknown reason.");
                return;
            }

            // Enumerate through files
            foreach (string path in Directory.EnumerateFiles(FolderPath, "*.mid", FileEnumOptions))
            {
                // Not really necessary to check, but just in case
                if (path.EndsWith(".mid"))
                {
                    Debug.WriteLine($"Editing file {path}");

                    // Get the .mid file
                    MidiFile Midi = null;
                    try
                    {
                        Midi = MidiFile.Read(path, MidiReadSettings);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not read .mid file at {path}:");
                        Console.WriteLine(ex.ToString());
                        continue;
                    }

                    // Overbearing safety part 2
                    if (Midi == null)
                    {
                        Console.WriteLine($"Could not read .mid file at {path} for an unknown reason.");
                        continue;
                    }

                    TrackChunk DrumsTrack = null;
                    TrackChunk RealDrumsTrack = null;

                    // Get the Drums and Real Drums tracks
                    foreach (TrackChunk track in Midi.GetTrackChunks())
                    {
                        foreach (MidiEvent trackEvent in track.Events)
                        {
                            if (trackEvent.EventType == MidiEventType.SequenceTrackName)
                            {
                                SequenceTrackNameEvent trackNameEvent = trackEvent as SequenceTrackNameEvent;
                                if (trackNameEvent.Text == "PART DRUMS")
                                {
                                    DrumsTrack = track;
                                    break;
                                }
                                else if (trackNameEvent.Text == "PART REAL_DRUMS_PS")
                                {
                                    RealDrumsTrack = track;
                                    break;
                                }
                            }
                        }
                    }

                    // Copy Real Drums to Drums if Drums doesn't exist
                    if (RealDrumsTrack != null)
                    {
                        if (DrumsTrack == null)
                        {
                            // Copy Real Drums track
                            DrumsTrack = (TrackChunk)RealDrumsTrack.Clone();

                            // Rename track
                            for (int i = 0; i < DrumsTrack.Events.Count; i++)
                            {
                                if (DrumsTrack.Events[i].EventType == MidiEventType.SequenceTrackName)
                                {
                                    ((SequenceTrackNameEvent)DrumsTrack.Events[i]).Text = "PART DRUMS";
                                }
                            }

                            // Add Drums track to the file and write the new file
                            Midi.Chunks.Add(DrumsTrack);
                            Midi.Write(path, overwriteFile: true);
                        }
                    }
                }
            }

            Console.WriteLine("Finished. Press any key to exit...");
            Console.ReadKey(intercept: true);
        }

        static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = args.ExceptionObject as Exception;
            Console.WriteLine("An unhandled exception has occured:");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(intercept: true);
        }
    }
}
