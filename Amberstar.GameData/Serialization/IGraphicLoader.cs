﻿using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Serialization;

public interface IGraphicLoader
{
	IGraphic[] LoadWindowGraphics(bool dark);
	IPaletteGraphic Load80x80Graphic(Image80x80 index);
	IGraphic LoadItemGraphic(ItemGraphic index);
	Dictionary<int, IGraphic> LoadAllBackgroundGraphics();
	Dictionary<int, IGraphic> LoadAllCloudGraphics();
	Dictionary<DayTime, Color[]> LoadSkyGradients();
    Dictionary<int, IGraphic> LoadPersonPortraits();
}
