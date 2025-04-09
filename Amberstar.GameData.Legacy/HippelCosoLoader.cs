﻿using Amber.Common;
using Amber.Serialization;
using System.Text;

namespace Amberstar.GameData.Legacy;

file class IndexTable
{
    public byte[][] Entries { get; }

    public IndexTable(IDataReader reader, int startOffset, int count, int end)
    {
        Entries = new byte[count][];
        var offsets = new int[count + 1];

        for (int i = 0; i < count; i++)
        {
            offsets[i] = reader.ReadWord();
        }

        offsets[count] = end;

        if (offsets[0] != reader.Position - startOffset)
            throw new AmberException(ExceptionScope.Data, "Invalid index table");

        for (int i = 0; i < count; i++)
        {
            Entries[i] = reader.ReadBytes(offsets[i + 1] - offsets[i]);
        }

        if (reader.Position != startOffset + end)
            throw new AmberException(ExceptionScope.Data, "Invalid index table");
    }
}

file record DivisionData
{
    // Atari ST uses 3 channels (voices), Amiga most likely uses 4 channels instead.
    public const int ChannelCount = 3;

    public record Channel
    {
        public byte Monopattern { get; }
        public sbyte Transpose { get; }
        public byte TimbreIndex { get; }
        public byte Effect { get; }

        public Channel(IDataReader dataReader)
        {
            Monopattern = dataReader.ReadByte();
            Transpose = unchecked((sbyte)dataReader.ReadByte());
            TimbreIndex = dataReader.ReadByte();
            Effect = dataReader.ReadByte();
        }
    }

    public Channel[] Channels { get; } = new Channel[ChannelCount];

    public DivisionData(IDataReader dataReader)
    {
        for (int i = 0; i < ChannelCount; i++)
        {
            Channels[i] = new Channel(dataReader);
        }
    }
}

internal static class HippelCosoLoader
{
    private readonly static byte[] CosoHeader = Encoding.ASCII.GetBytes("COSO"); // COmpressed SOng
    private readonly static byte[] TfmxHeader = Encoding.ASCII.GetBytes("TFMX"); // The Final Musicsystem eXtended
    private readonly static byte[] MmmeHeader = Encoding.ASCII.GetBytes("MMME"); // Mad Max Music Editor
    private readonly static byte[] LsmpHeader = Encoding.ASCII.GetBytes("LSMP");

    private static bool BytesMatch(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    private static T ReadAt<T>(IDataReader reader, int offset, Func<IDataReader, T> readFunc)
    {
        reader.Position = offset;

        return readFunc(reader);
    }

    public static List<HippelCosoSong> Load(IDataReader dataReader)
    {
        var position = dataReader.Position;
        var header = dataReader.ReadBytes(4);

        if (BytesMatch(header, CosoHeader))
        {
            dataReader.Position = position;
            return LoadCoso(dataReader);
        }
        else if (BytesMatch(header, TfmxHeader))
        {
            if (ReadAt(dataReader, position + 0x4, r => r.ReadWord()) >= 0x200 ||
                ReadAt(dataReader, position + 0x10, r => r.ReadWord()) == 0)
            {
                throw new AmberException(ExceptionScope.Data, "Invalid TFMX file");
            }

            dataReader.Position = position;
            return LoadTfmx(dataReader);
        }
        else if (BytesMatch(header, MmmeHeader))
        {
            dataReader.Position = position;
            return LoadMmme(dataReader);
        }
        else
        {
            throw new AmberException(ExceptionScope.Data, "Unknown CoSo header");
        }
    }

    private static List<HippelCosoSong> LoadCoso(IDataReader dataReader)
    {
        // Load COmpressed SOng

        var position = dataReader.Position;
        const string error = "Invalid CoSo file";

        dataReader.Position += 0x30;

        if (dataReader.ReadWord() == 0) // Number of songs
            throw new AmberException(ExceptionScope.Data, error);

        dataReader.Position = position + 0x18;

        if (dataReader.ReadDword() == 0) // Offset to samples
            throw new AmberException(ExceptionScope.Data, error);

        dataReader.Position = position + 0x20;

        var header = dataReader.ReadBytes(4);
        dataReader.Position = position;

        if (BytesMatch(header, TfmxHeader))
        {
            return LoadTfmx(dataReader);
        }
        else if (BytesMatch(header, MmmeHeader))
        {
            return LoadMmme(dataReader);
        }
        else
        {
            throw new AmberException(ExceptionScope.Data, error);
        }
    }

    // Load The Final Musicsystem eXtended
    private static List<HippelCosoSong> LoadTfmx(IDataReader dataReader) => Load(dataReader, false);

    // Load Mad Max Music Editor
    private static List<HippelCosoSong> LoadMmme(IDataReader dataReader) => Load(dataReader, true);

    private static List<HippelCosoSong> Load(IDataReader dataReader, bool isMmme)
    {
        // NOTE: In some cases there are 4 byte indices used which also changes
        // some other logic. Most likely some other file format version. But
        // we assume the 2 byte indices version here.

        string error = $"Invalid {(isMmme ? "MMME" : "TFMX")} file";
        var position = dataReader.Position;

        dataReader.Position += 0x1c;

        if (BytesMatch(dataReader.ReadBytes(4), LsmpHeader))
        {
            throw new AmberException(ExceptionScope.Data, "LSMP is not supported");
        }

        dataReader.Position = position + 0x4;

        var posInstruments = dataReader.ReadDword();
        var posTimbres = dataReader.ReadDword();
        var posMonopatterns = dataReader.ReadDword();
        var posDivisions = dataReader.ReadDword();
        var posSongs = dataReader.ReadDword();
        var posSamples = dataReader.ReadDword();
        var totalSize = dataReader.ReadDword();

        dataReader.Position += 4;

        var numInstruments = dataReader.ReadWord() + 1;
        var numTimbres = dataReader.ReadWord() + 1;
        var numMonopatterns = dataReader.ReadWord() + 1;
        var numDivisions = dataReader.ReadWord() + 1;
        var bytesPerMonopattern = dataReader.ReadWord();

        dataReader.Position += 2;

        var numSongs = dataReader.ReadWord();
        var numSamples = dataReader.ReadWord();

        if (numSongs == 0)
            throw new AmberException(ExceptionScope.Data, error);

        int sizePerSample = isMmme ? 6 : 10;

        if (posSamples + (numSamples + 1) * sizePerSample != totalSize)
            throw new AmberException(ExceptionScope.Data, error);

        dataReader.Position = position + (int)posInstruments;
        var instrumentTable = new IndexTable(dataReader, position, numInstruments, (int)posTimbres);

        dataReader.Position = position + (int)posTimbres;
        var timbresTable = new IndexTable(dataReader, position, numTimbres, (int)posMonopatterns);

        dataReader.Position = position + (int)posMonopatterns;
        var monpatternTable = new IndexTable(dataReader, position, numMonopatterns, (int)posDivisions);

        dataReader.Position = position + (int)posDivisions;
        var divisionData = new DivisionData[numDivisions];

        for (int i = 0; i < numDivisions; i++)
        {
            divisionData[i] = new DivisionData(dataReader);
        }

        dataReader.Position = position + (int)posSongs;

        var songs = new HippelCosoSong.SongInfo[numSongs];

        for (int i = 0; i < numSongs; i++)
        {
            songs[i] = new(dataReader.ReadWord(), dataReader.ReadWord(), dataReader.ReadWord());
        }

        // The data stores numSongs + 1 entries but the last is just full of zeros.

        dataReader.Position = position + (int)posSamples;

        // TODO: samples
        for (int i = 0; i < numSamples; i++)
        {
            var data = dataReader.ReadBytes(sizePerSample);

            Console.WriteLine();
        }

        // The data stores numSamples + 1 entries but the last is just full of zeros.

        // Create data structures
        var instruments = new HippelCosoSong.Instrument[numInstruments];
        var timbres = new HippelCosoSong.Timbre[numTimbres];
        var divisions = new HippelCosoSong.Division[numDivisions];
        var patterns = new HippelCosoSong.Pattern[numMonopatterns];

        for (int i = 0; i < numInstruments; i++)
        {
            var instrumentData = instrumentTable.Entries[i];
            var commands = new List<HippelCosoSong.Instrument.Command>();

            for (int b = 0; b < instrumentData.Length; b++)
            {
                var command = instrumentData[b];

                switch (command)
                {
                    case 0xe0:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.Loop, instrumentData[++b]));
                        break;
                    case 0xe1:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.Complete));
                        break;
                    case 0xe2:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.ResetTimbre));
                        break;
                    case 0xe3:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.Vibrato, instrumentData[++b], instrumentData[++b]));
                        break;
                    case 0xe4:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.EnableToneAndNoise, instrumentData[++b]));
                        break;
                    case 0xe5:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.DisableToneEnableNoise));
                        break;
                    case 0xe6:
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.EnableToneDisableNoise));
                        break;
                    case 0xe7: // Set timbre
                        // Note: In some versions (with 4 byte indices) this defaults to command ed instead.
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.SetTimbre, instrumentData[++b]));
                        break;
                    case 0xe8: // Delay
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.Delay, instrumentData[++b]));                        
                        break;
                    case 0xe9:
                        // It seems this is just skipping data and immediately processes the next command.
                        b++;
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.NextCommand));
                        break;
                    case 0xea: // Portando (arg = slope)
                        // Note: In some versions (with 4 byte indices) this defaults to command ec instead.
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.Portando, instrumentData[++b]));
                        break;
                    case 0xeb:
                        break;
                    case 0xec:
                        break;
                    case 0xed:
                        break;
                    case 0xee:
                        break;
                    case 0xef:
                        break;
                    default: // Pitch
                        commands.Add(new(HippelCosoSong.Instrument.CommandType.SetAbsolutePitch, command));
                        break;
                }
            }

            instruments[i] = new([..commands]);
        }

        for (int i = 0; i < numTimbres; i++)
        {
            var timbreData = timbresTable.Entries[i];
            int b = 0;

            int speed = timbreData[b++];
            int instrument = timbreData[b++];
            int vibratoSlope = timbreData[b++];
            int vibratoDepth = timbreData[b++];
            int vibratoDelay = timbreData[b++];

            var commands = new List<HippelCosoSong.VolumeEnvelop.Command>();

            for (; b < timbreData.Length; b++)
            {
                var command = timbreData[b];

                if (command < 0xe0)
                {
                    commands.Add(new(HippelCosoSong.VolumeEnvelop.CommandType.SetVolume, command));
                }
                else if (command == 0xe0) // LOOP(offset)
                {
                    var offset = timbreData[++b] - 5;
                    commands.Add(new(HippelCosoSong.VolumeEnvelop.CommandType.Loop, offset));
                }
                else if (command == 0xe1) // HOLD
                {
                    commands.Add(new(HippelCosoSong.VolumeEnvelop.CommandType.Hold));
                }
                else if (command == 0xe8) // SUSTAIN(ticks)
                {
                    var ticks = timbreData[++b];
                    commands.Add(new(HippelCosoSong.VolumeEnvelop.CommandType.Sustain, ticks));
                }
                else if(command > 0xe8)
                {
                    // e2 to e7 are NOOP operations, but above e8 should not happen.
                    throw new AmberException(ExceptionScope.Data, "Invalid volume envelop command");
                }
            }

            timbres[i] = new(speed, instrument, new(vibratoSlope, vibratoDepth, vibratoDelay), new(speed, [..commands]));
        }

        for (int i = 0; i < numDivisions; i++)
        {
            var data = divisionData[i];
            var channels = new HippelCosoSong.Division.Channel[data.Channels.Length];

            for (int c = 0; c < data.Channels.Length; c++)
            {
                int volumeReduction = 0;
                int speedFactor = 1;
                int timbreAdjust = 0;

                var effect = data.Channels[c].Effect;

                if ((effect & 0xf0) == 0xf0)
                {
                    volumeReduction = effect & 0xf;
                }
                else if ((effect & 0xf0) == 0xe0)
                {
                    speedFactor = 1 + effect & 0xf;
                }
                else if ((effect & 0x80) == 0)
                {
                    timbreAdjust = effect;
                }

                channels[c] = new(data.Channels[c].Monopattern, data.Channels[c].Transpose, data.Channels[c].TimbreIndex,
                    volumeReduction, speedFactor, timbreAdjust);
            }

            divisions[i] = new(channels);
        }

        for (int i = 0; i < numMonopatterns; i++)
        {
            var patternData = monpatternTable.Entries[i];
            var commands = new List<HippelCosoSong.Pattern.Command>();

            for (int b = 0; b < patternData.Length; b++)
            {
                var command = patternData[b];

                if (command == 0xff)
                {
                    commands.Add(new(HippelCosoSong.Pattern.CommandType.EndPattern));
                }
                else if (command == 0xfe)
                {
                    commands.Add(new(HippelCosoSong.Pattern.CommandType.SetSpeed, patternData[++b]));
                }
                else if (command == 0xfd)
                {
                    commands.Add(new(HippelCosoSong.Pattern.CommandType.SetSpeedWithDelay, patternData[++b]));
                }
                else // notes
                {
                    var note = command;
                    var arg = patternData[++b];
                    int instrument = -1;
                    int timbreAdjust = -1;

                    if ((arg & 0xe0) != 0)
                    {
                        instrument = patternData[++b];
                    }

                    if ((arg & 0x80) == 0)
                    {
                        timbreAdjust = arg & 0x1f;

                        if ((arg & 0x40) != 0)
                            instrument = -1;
                    }

                    commands.Add(new(HippelCosoSong.Pattern.CommandType.SetNote, note, timbreAdjust, instrument));
                }
            }

            patterns[i] = new([.. commands]);
        }

        return [..songs.Select(song => new HippelCosoSong(song, instruments, timbres, divisions, patterns))];
    }
}
