﻿using Amber.Assets.Common;
using Amber.Serialization;
using System.Runtime.InteropServices;

namespace Amberstar.GameData.Legacy;

// If blind, all dark.
// Otherwise if Light bit is 1, just show full brightness.
// If Light bit is 0, check for LightChange bit.
// If 1, get the light radius by the hour. Use this table:
// 16,16,16,16,16,16,40,64, 9x 200, 64,64,40,16,16,16,16,16
// Any active light spell will add its effect (lsl by 3) to the radius.
// If change bit was 0 instead, check the dark bit. If this is not set,
// just do nothing (no change).
// Otherwise check travel type. Superchicken mode grants full light.
// Otherwise if no light spell is active, use full darkness.
// Otherwise use a radius of 16 + (lsl by 3 the spell effect).

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct MapHeader
{
	public byte Magic; // 0xff
	public byte Fill; // 0x00
	public word Tileset;
	public MapType MapType; // 0: 2D, 1: 3D
	public MapFlags Flags;
	public byte Music;
	public byte Width;
	public byte Height;
	public fixed byte Name[31]; // null-terminated (max 30 chars)
	public fixed byte EventData[IMap.EventCount * IEvent.DataSize];
	public fixed word CharacterData[IMap.CharacterCount]; // Person index, monster group index, map text index
	public fixed byte CharacterIcon[IMap.CharacterCount];
	public fixed byte CharacterMove[IMap.CharacterCount];
	public fixed byte CharacterFlags[IMap.CharacterCount];
	public fixed byte CharacterDay[IMap.CharacterCount];
	public fixed byte CharacterMonth[IMap.CharacterCount];
	public word StepsPerDay; // 288
	public byte MonthsPerYear; // 12
	public byte DaysPerMonth; // 30
	public byte HoursPerDay; // 24
	public byte MinutesPerHour; // 60
	public byte MinutesPerStep; // 5
	public byte HoursPerDaytime; // 12
	public byte HoursPerNighttime; // 12
	public readonly word LabdataIndex => Tileset;
}

internal abstract class Map : IMap
{
    protected readonly MapHeader header;

	public unsafe Map(MapHeader header, MapCharacter[] characters, PositionList[] characterPositions)
	{
		this.header = header;
		Characters = characters;
		CharacterPositions = characterPositions;

        Name = new string((sbyte*)header.Name).TrimEnd(' ', '\0');

		var eventReader = new FixedDataReader(header.EventData, IMap.EventCount * IEvent.DataSize);

		for (int i = 0; i < IMap.EventCount; i++)
		{
			if (eventReader.PeekByte() == 0)
			{
				// No event here
				eventReader.Position += IEvent.DataSize;
				continue;
			}

			Events.Add(Event.ReadEvent(eventReader));
		}
	}

	public static unsafe Map Load(IAsset asset)
	{
		var reader = asset.GetReader();
		var headerData = reader.ReadBytes(sizeof(MapHeader));

        if (BitConverter.IsLittleEndian)
		{
			var builder = new StructEndianessFixer.Builder();
			var fixer = builder
				.Word(2)
				.WordGap(40 + 6, 2, IMap.EventCount, 6)
				.WordArray(40 + 2540, IMap.CharacterCount)
				.Word(40 + 2540 + 7 * IMap.CharacterCount)
				.Build();
			fixer.FixData(headerData);
		}

		MapHeader header;

		fixed (byte* ptr = headerData)
		{
			header = *(MapHeader*)ptr;
		}

		if (header.MapType == MapType.Map2D)
			return Map2D.Load(asset.Identifier.Index, header, reader);
		else
			return Map3D.Load(asset.Identifier.Index, header, reader);
	}

	public int Width => header.Width;

	public int Height => header.Height;

	public MapType Type => header.MapType;

	public MapFlags Flags => header.Flags;

	public string Name { get; }

    public MapCharacter[] Characters { get; }

	public PositionList[] CharacterPositions { get; }

	public List<IEvent> Events { get; } = [];
}