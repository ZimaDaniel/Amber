﻿using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

public interface IGraphicIndexProvider
{
	int GetUIGraphicIndex(UIGraphic graphic);
	int GetButtonIndex(ButtonType button);
	int GetStatusIconIndex(StatusIcon statusIcon);
	int Get80x80ImageIndex(Image80x80 image);
	int GetItemGraphicIndex(ItemGraphic graphic);
	int GetWindowGraphicIndex(bool dark);
	int GetBackgroundGraphicIndex(int index);
	int GetCloudGraphicIndex(int index);
	int GetLabBlockGraphicIndex(int labBlockIndex, PerspectiveLocation location, BlockFacing facing);
	int GetCursorGraphicIndex(CursorType cursorType);
	int GetPersonPortraitIndex(int index);
}
