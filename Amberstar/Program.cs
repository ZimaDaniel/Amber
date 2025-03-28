﻿using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.FileSystem;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Amberstar
{
	internal class Program
	{
		static void Main(string[] args)
		{
            //string basePath = @"D:\Projects\Amber\English\AmberfilesST";
            string basePath = @"D:\Projects\Amber\German\AmberfilesST";

			var fileSystem = FileSystem.FromOperatingSystemPath(basePath);

			var assetProvider = new AssetProvider(fileSystem.AsReadOnly());

			void WriteTexts(AssetType assetType, int count, int start = 0, bool skipEmpty = false, bool textBlocks = false)
			{
				string folder = $@"{basePath}\{assetType}s";
				Directory.CreateDirectory(folder);

				for (int i = 0; i < count; i++)
				{
					var assetId = new AssetIdentifier(assetType, start + i);
					var text = assetProvider.TextLoader.LoadText(assetId);

					if (textBlocks)
					{
						if (skipEmpty && (text.TextBlockCount == 0 || string.IsNullOrWhiteSpace(text.GetString())))
							continue;

						var subFolder = count == 1 ? folder : Path.Combine(folder, $"{start + i:000}");

						Directory.CreateDirectory(subFolder);

						for (int b = 0; b < text.TextBlockCount; b++)
						{
							var content = string.Join('\n', text.GetTextBlock(b).GetLines(int.MaxValue));
							File.WriteAllText(Path.Combine(subFolder, $"{b:000}.txt"), content, System.Text.Encoding.UTF8);
						}
					}
					else
					{
						var content = string.Join('\n', text.GetLines(int.MaxValue));

						if (skipEmpty && string.IsNullOrWhiteSpace(content))
							continue;

						File.WriteAllText(Path.Combine(folder, $"{start + i:000}.txt"), content, System.Text.Encoding.UTF8);
					}
				}
			}

			WriteTexts(AssetType.ClassName, 11);
            WriteTexts(AssetType.RaceName, 15);
            WriteTexts(AssetType.AttributeName, 9);
            WriteTexts(AssetType.SkillName, 10);
			WriteTexts(AssetType.CharInfoText, 5);
			WriteTexts(AssetType.LanguageName, 7);
			WriteTexts(AssetType.ConditionName, 16, 1);
			WriteTexts(AssetType.ItemTypeName, 19);
			WriteTexts(AssetType.SpellSchoolName, 7, 1);
			WriteTexts(AssetType.SpellName, 7*30, 1);
			WriteTexts(AssetType.MapText, 152, 1, true, true);
			WriteTexts(AssetType.PuzzleText, 1, 1, false, true);
			WriteTexts(AssetType.ItemText, 2, 1, false, true);
            WriteTexts(AssetType.UIText, Enum.GetValues<UIText>().Length);

            byte[] uiPalette = assetProvider.PaletteLoader.LoadBuiltinPalette(BuiltinPalette.UI).GetData();
            byte[] itemPalette = assetProvider.PaletteLoader.LoadBuiltinPalette(BuiltinPalette.Item).GetData();

            for (int i = 1; i <= 11; i++)
			{
				var layout = assetProvider.LayoutLoader.LoadLayout(i);
				WriteGraphic($@"{basePath}\Layout\{i:000}.png", layout, uiPalette, false);
			}

			WriteGraphic($@"{basePath}\Layout\PortraitArea.png", assetProvider.LayoutLoader.LoadPortraitArea(), uiPalette, false);

			for (int i = 0; i <= (int)UIGraphic.LastUIGraphic; i++)
			{
				var graphic = (UIGraphic)i;
				WriteGraphic($@"{basePath}\UIGraphics\{graphic}.png", assetProvider.UIGraphicLoader.LoadGraphic(graphic), uiPalette, false);
			}

			for (int i = 0; i <= (int)ButtonType.LastButton; i++)
			{
				var button = (ButtonType)i;
				WriteGraphic($@"{basePath}\Buttons\{button}.png", assetProvider.UIGraphicLoader.LoadButtonGraphic(button), uiPalette, false);
			}

			for (int i = 0; i <= (int)StatusIcon.LastStatusIcon; i++)
			{
				var statusIcon = (StatusIcon)i;
				WriteGraphic($@"{basePath}\StatusIcons\{statusIcon}.png", assetProvider.UIGraphicLoader.LoadStatusIcon(statusIcon), uiPalette, false);
			}

			for (int i = 1; i <= (int)Image80x80.LastImage; i++)
			{
				var image = (Image80x80)i;
				var graphic = assetProvider.GraphicLoader.Load80x80Graphic(image);
				WriteGraphic($@"{basePath}\80x80Images\{i:000}.png", graphic, graphic.Palette.GetData(), false);
			}

			var backgrounds = assetProvider.GraphicLoader.LoadAllBackgroundGraphics();
			foreach (var background in backgrounds)
			{
				WriteGraphic($@"{basePath}\Backgrounds\{background.Key:000}.png", background.Value, uiPalette, false);
			}

			var labBlocks = assetProvider.LabDataLoader.LoadAllLabBlocks();

			foreach (var labBlock in labBlocks)
			{
				for (int i = 0; i < labBlock.Value.Perspectives.Length; i++)
					WriteGraphic($@"{basePath}\LabBlocks\{labBlock.Key:000}\Perspective{i:000}.png", labBlock.Value.Perspectives[i].Frames.ToGraphic(), uiPalette, true);
			}

            for (int i = 0; i <= (int)ItemGraphic.LastItemGraphic; i++)
			{
                var graphic = (ItemGraphic)i;
                WriteGraphic($@"{basePath}\Items\{graphic}.png", assetProvider.GraphicLoader.LoadItemGraphic(graphic), itemPalette, true);
            }
        }

		static void WriteGraphic(string filename, IGraphic graphic, byte[] palette, bool transparency)
		{
			var dir = Path.GetDirectoryName(filename);

			Directory.CreateDirectory(dir);

			using var bitmap = new Bitmap(graphic.Width, graphic.Height);
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			byte[] index0Color = [0, 0, 0, (byte)(transparency ?  0 : 0xff)];

			// Out: B G R A
			//  In: R G B A
			byte[] PalIndexToColor(int index)
			{
				if (index == 0)
					return index0Color;

				return [ palette[index * 4 + 2], palette[index * 4 + 1], palette[index * 4 + 0], 0xff];
			}

			var pixels = graphic.GetData().SelectMany(paletteIndex => PalIndexToColor(paletteIndex)).ToArray();

			Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);

			bitmap.UnlockBits(data);
			bitmap.Save(filename);
		}
	}
}
