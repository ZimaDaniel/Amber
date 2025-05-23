﻿using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class GraphicLoader(AssetProvider assetProvider) : IGraphicLoader
{
	private readonly IGraphic[]?[] windowGraphics = [null, null];
	private readonly Dictionary<Image80x80, IPaletteGraphic> graphics80x80 = [];
	private readonly Dictionary<ItemGraphic, IGraphic> itemGraphics = [];
	private readonly Dictionary<int, IGraphic> backgroundGraphics = [];
	private readonly Dictionary<int, IGraphic> cloudGraphics = [];
	private readonly Dictionary<DayTime, Color[]> skyGradients = [];
    private readonly Dictionary<int, IGraphic> personPortraits = [];

    public static byte[] LoadGraphicDataWithHeader(IDataReader dataReader, out int width, out int height, out int planes)
	{
		width = dataReader.ReadWord() + 1;
		height = dataReader.ReadWord() + 1;
		planes = dataReader.ReadWord();

		if (planes != 4)
			throw new AmberException(ExceptionScope.Data, "Unexpected plane count for legacy graphic data.");

		int size;

		if (width % 16 != 0) // Fix byte count in this case
			size = (width + 16 - width % 16) * height * planes / 8;
		else
			size = width * height * planes / 8;

		return dataReader.ReadBytes(size);
	}

	public static Graphic LoadGraphicWithHeader(IDataReader dataReader)
	{
		var data = LoadGraphicDataWithHeader(dataReader, out int width, out int height, out int planes);
		int readWidth = width;

		if (readWidth % 16 != 0)
			readWidth += 16 - readWidth % 16;

		var graphic = Graphic.FromBitPlanes(readWidth, height, data, planes);

		if (width < readWidth)
			return graphic.GetPart(0, 0, width, height);

		return graphic;
	}

	public static Graphic[] LoadGraphicList(IDataReader dataReader)
	{
		long totalDataSize = dataReader.ReadDword();
		int numGraphics = dataReader.ReadByte();

		if (dataReader.ReadByte() != 0)
			throw new AmberException(ExceptionScope.Data, "Invalid graphic list.");

		var graphics = new Graphic[numGraphics];

		for (int i = 0; i < numGraphics; i++)
		{
			var graphicDataSize = dataReader.ReadDword();
			var expectedEndOffset = dataReader.Position + graphicDataSize;
			graphics[i] = LoadGraphicWithHeader(dataReader);

			if (dataReader.Position != expectedEndOffset)
				throw new AmberException(ExceptionScope.Data, "Invalid graphic list.");

			totalDataSize -= graphicDataSize;
		}

		if (totalDataSize != 1)
			throw new AmberException(ExceptionScope.Data, "Invalid graphic list.");

		return graphics;
	}

	public static PaletteGraphic LoadGraphicWithHeader(IDataReader dataReader, IGraphic palette)
	{
		var data = LoadGraphicDataWithHeader(dataReader, out int width, out int height, out int planes);

		return PaletteGraphic.FromBitPlanes(width, height, data, planes, palette);
	}

	public IGraphic[] LoadWindowGraphics(bool dark)
	{
		int index = dark ? 0 : 1;

		if (windowGraphics[index] != null)
			return windowGraphics[index]!;

		var asset = assetProvider.GetAsset(new(AssetType.Window, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Window graphics {index} not found.");

		// Load graphics
		var reader = asset.GetReader();
		int graphicSize = 16 * 16 * 4 / 8;
		int graphicCount = reader.Size / graphicSize;
		var graphics = new IGraphic[graphicCount];

		for (int i = 0; i < graphics.Length; i++)
			graphics[i] = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(graphicSize), 4);

		return windowGraphics[index] = graphics;
	}

	public IPaletteGraphic Load80x80Graphic(Image80x80 index)
	{
		if (graphics80x80.TryGetValue(index, out var graphic))
			return graphic;

		// Each even file is the next image.
		// Each odd file is the palette for the last image.	

		var asset = assetProvider.GetAsset(new(AssetType.Graphics80x80, (int)index * 2 - 1));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"80x80 graphic {index} not found.");

		// Read palette
		var paletteAsset = assetProvider.GetAsset(new(AssetType.Graphics80x80, (int)index * 2));

		if (paletteAsset == null)
			throw new AmberException(ExceptionScope.Data, $"Palette for 80x80 graphic {index} not found.");

		var palette = PaletteLoader.LoadPalette(paletteAsset.GetReader());

		// Load graphic
		graphic = PaletteGraphic.FromBitPlanes(80, 80, asset.GetReader().ReadToEnd(), 4, palette);

		graphics80x80.Add(index, graphic);

		return graphic;
	}

	private void LoadBackgroundsAndClouds()
	{
		foreach (var key in assetProvider.GetAssetKeys(AssetType.Background))
		{
			var asset = assetProvider.GetAsset(new(AssetType.Background, key));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Background {key} not found.");

			var reader = asset.GetReader();

			// Load graphics
			var graphics = GraphicLoader.LoadGraphicList(reader);

			backgroundGraphics.Add(key, graphics[0]);

			if (graphics.Length > 1)
				cloudGraphics.Add(key, graphics[1]);
		}
	}

	public Dictionary<int, IGraphic> LoadAllBackgroundGraphics()
	{
		if (backgroundGraphics.Count != 0)
			return backgroundGraphics;

		LoadBackgroundsAndClouds();

		return backgroundGraphics;
	}

	public Dictionary<int, IGraphic> LoadAllCloudGraphics()
	{
		if (cloudGraphics.Count != 0)
			return cloudGraphics;

		LoadBackgroundsAndClouds();

		return cloudGraphics;
	}

	public IGraphic LoadItemGraphic(ItemGraphic index)
	{
		if (itemGraphics.TryGetValue(index, out var graphic))
			return graphic;

		var asset = assetProvider.GetAsset(new(AssetType.ItemGraphic, (int)index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Item graphic {index} not found.");

		var reader = asset.GetReader();

		// Load graphic
		graphic = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(16 * 16 / 2), 4);

		itemGraphics.Add(index, graphic);

		return graphic;
	}

	public Dictionary<DayTime, Color[]> LoadSkyGradients()
	{
		if (skyGradients.Count != 0)
			return skyGradients;

		for (int i = 0; i < 3; i++)
		{
			var asset = assetProvider.GetAsset(new(AssetType.SkyGradient, i));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Sky gradient {i} not found.");

			var reader = asset.GetReader();
			var gradient = new Color[84];
			var colorData = PaletteLoader.LoadPaletteColors(reader, 84);
			int colorDataIndex = 0;

			for (int n = 0; n < gradient.Length; n++)
			{
				var r = colorData[colorDataIndex++] & 0xf;
				var gb = colorData[colorDataIndex++];
				var g = gb >> 4;
				var b = gb & 0xf;
				r |= (r << 4);
				g |= (g << 4);
				b |= (b << 4);

				gradient[n] = new Color((byte)r, (byte)g, (byte)b);
			}

            // 0 -> 2 (first is day)
            // 1 -> 0 (second is night)
            // 2 -> 1 (third is dawn/dusk)
            DayTime dayTime = (DayTime)((i + 2) % 3);

			skyGradients.Add(dayTime, gradient);

			if (dayTime == DayTime.Dawn)
				skyGradients.Add(DayTime.Dusk, gradient);
		}

		return skyGradients;
	}

    public Dictionary<int, IGraphic> LoadPersonPortraits()
	{
		if (personPortraits.Count != 0)
            return personPortraits;

        foreach (var key in assetProvider.GetAssetKeys(AssetType.Person))
        {
            var person = assetProvider.PersonLoader.LoadPerson(key);

            if (person.Portrait != null)
                personPortraits.Add(key, person.Portrait);
        }

		return personPortraits;
    }
}
