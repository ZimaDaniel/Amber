﻿using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class Map3DScreen : ButtonGridScreen
{
	private static readonly Dictionary<Direction, Dictionary<PerspectiveLocation, Position>> PerspectiveMappings =
		new()
		{
			{
				Direction.North, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(-1, -3) },
					{ PerspectiveLocation.Forward3Right1, new Position(1, -3) },
					{ PerspectiveLocation.Forward3, new Position(0, -3) },
					{ PerspectiveLocation.Forward2Left1, new Position(-1, -2) },
					{ PerspectiveLocation.Forward2Right1, new Position(1, -2) },
					{ PerspectiveLocation.Forward2, new Position(0, -2) },
					{ PerspectiveLocation.Forward1Left1, new Position(-1, -1) },
					{ PerspectiveLocation.Forward1Right1, new Position(1, -1) },
					{ PerspectiveLocation.Forward1, new Position(0, -1) },
					{ PerspectiveLocation.Left1, new Position(-1, 0) },
					{ PerspectiveLocation.Right1, new Position(1, 0) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(-2, -3) },
					{ PerspectiveLocation.Forward3Right2, new Position(2, -3) },
				}
			},
			{
				Direction.East, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(3, -1) },
					{ PerspectiveLocation.Forward3Right1, new Position(3, 1) },
					{ PerspectiveLocation.Forward3, new Position(3, 0) },
					{ PerspectiveLocation.Forward2Left1, new Position(2, -1) },
					{ PerspectiveLocation.Forward2Right1, new Position(2, 1) },
					{ PerspectiveLocation.Forward2, new Position(2, 0) },
					{ PerspectiveLocation.Forward1Left1, new Position(1, -1) },
					{ PerspectiveLocation.Forward1Right1, new Position(1, 1) },
					{ PerspectiveLocation.Forward1, new Position(1, 0) },
					{ PerspectiveLocation.Left1, new Position(0, -1) },
					{ PerspectiveLocation.Right1, new Position(0, 1) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(3, -2) },
					{ PerspectiveLocation.Forward3Right2, new Position(3, 2) },
				}
			},
			{
				Direction.South, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(1, 3) },
					{ PerspectiveLocation.Forward3Right1, new Position(-1, 3) },
					{ PerspectiveLocation.Forward3, new Position(0, 3) },
					{ PerspectiveLocation.Forward2Left1, new Position(1, 2) },
					{ PerspectiveLocation.Forward2Right1, new Position(-1, 2) },
					{ PerspectiveLocation.Forward2, new Position(0, 2) },
					{ PerspectiveLocation.Forward1Left1, new Position(1, 1) },
					{ PerspectiveLocation.Forward1Right1, new Position(-1, 1) },
					{ PerspectiveLocation.Forward1, new Position(0, 1) },
					{ PerspectiveLocation.Left1, new Position(1, 0) },
					{ PerspectiveLocation.Right1, new Position(-1, 0) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(2, 3) },
					{ PerspectiveLocation.Forward3Right2, new Position(-2, 3) },
				}
			},
			{
				Direction.West, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(-3, 1) },
					{ PerspectiveLocation.Forward3Right1, new Position(-3, -1) },
					{ PerspectiveLocation.Forward3, new Position(-3, 0) },
					{ PerspectiveLocation.Forward2Left1, new Position(-2, 1) },
					{ PerspectiveLocation.Forward2Right1, new Position(-2, -1) },
					{ PerspectiveLocation.Forward2, new Position(-2, 0) },
					{ PerspectiveLocation.Forward1Left1, new Position(-1, 1) },
					{ PerspectiveLocation.Forward1Right1, new Position(-1, -1) },
					{ PerspectiveLocation.Forward1, new Position(-1, 0) },
					{ PerspectiveLocation.Left1, new Position(0, 1) },
					{ PerspectiveLocation.Right1, new Position(0, -1) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(-3, 2) },
					{ PerspectiveLocation.Forward3Right2, new Position(-3, -2) },
				}
			}
		};

	enum ButtonLayout
	{
		Movement,
		Actions
	}

	class Character
	{
		private readonly IMap3D map;
		private readonly MapCharacter data;
		private readonly Position[] positions;
		private readonly Func<int, int, int, bool> canMoveChecker;
		private int currentPathLength = 0;
		private Direction direction = Direction.North;
		private Position position; // this one is 1-based

		public int Index { get; }

        public int CharacterIndex => data.Index;

        public MapCharacterType Type => data.Type;

		public Position Position => new(position.X - 1, position.Y - 1);

		public int Icon => data.Icon;

		public Character(IMap3D map, int index, Position[] positions, GameState gameState,
			Func<int, int, int, bool> canMoveChecker)
		{
			this.map = map;
			Index = index;
			data = map.Characters[index];
			this.positions = positions;
			this.canMoveChecker = canMoveChecker;

			UpdatePosition(gameState);
		}

		private void UpdatePosition(GameState gameState)
		{
			void TryWalkTo(Position position)
			{
				if (canMoveChecker(position.X - 1, position.Y - 1, data.TravelType))
					this.position = position;
			}

			switch (data.WalkType)
			{
				case MapCharacterWalkType.Stationary:
					position = positions[0];
					break;
				case MapCharacterWalkType.Path:
				{
					int totalSteps = gameState.Hour * 12 + gameState.Minute / 5;
					TryWalkTo(positions[totalSteps]);
					break;
				}
				case MapCharacterWalkType.Chase:
					// TODO
					break;
				default: // random
					if (position == new Position()) // first time
						position = positions[0];
					else
						MoveRandomly();
					break;
			}
		}

		private void SetupNewRandomPath()
		{
			currentPathLength = Game.Random(1, 4);
			int dir = (int)direction + 1;
			dir += Game.Random(0, 1);
			dir &= 0x3;
			direction = (Direction)dir;

			switch (direction)
			{
				case Direction.North:
					if (currentPathLength <= position.Y)
						currentPathLength += position.Y - currentPathLength - 1;
					break;
				case Direction.East:
					if (currentPathLength > map.Width - position.X)
						currentPathLength += map.Width - position.X - currentPathLength - 1;
					break;
				case Direction.South:
					if (currentPathLength > map.Height - position.Y)
						currentPathLength += map.Height - position.Y - currentPathLength - 1;
					break;
				case Direction.West:
					if (currentPathLength <= position.X)
						currentPathLength += position.X - currentPathLength - 1;
					break;
			}
		}

		private void MoveRandomly()
		{
			if (currentPathLength == 0)
				SetupNewRandomPath();

			for (int i = 0; i < 4; i++)
			{
				// 4 tries
				var offset = direction.Offset();

				if (canMoveChecker(Position.X + offset.X, Position.Y + offset.Y, data.TravelType))
				{
					currentPathLength--;
					position = new(position.X + offset.X, position.Y + offset.Y);
					return;
				}

				SetupNewRandomPath();
			}

			currentPathLength = 0;
		}
		
		public void Update(Game game)
		{
			UpdatePosition(game.State);
		}
	}

	const int TicksPerStep = 16;
	const int TicksPerTurn = 16;
	const int AnimationTicksPerFrame = 25;

	const int ViewWidth = 144;
	const int ViewHeight = 144;
	const int OffsetX = 32;
	const int OffsetY = 49;
	const int SkyTransparentColorIndex = 11;
	Dictionary<int, IGraphic> backgrounds = [];
	Dictionary<int, IGraphic> clouds = [];	
	Dictionary<DayTime, Color[]> skyGradients = [];
	Game? game;
	IMap3D? map;
	ILabData? labData;
	readonly List<IColoredRect> skyGradient = [];
	readonly List<IAnimatedSprite> images = [];
	readonly List<Character> characters = [];
	ButtonLayout buttonLayout = ButtonLayout.Movement;
	long currentTicks = 0;
	long lastMoveTicks = 0;
	long lastTurnTicks = 0;
	long lastAnimationFrame = 0;
	byte palette = 0;
	bool mouseDown = false;
	IRenderText? mapNameText;

	public override ScreenType Type { get; } = ScreenType.Map3D;
	public IMap3D Map => map!;

	protected override byte ButtonGridPaletteIndex => palette;

    internal void MapChanged()
	{
		LoadMap(game!.State.MapIndex);
		AfterMove();
	}

	public override void Init(Game game)
	{
		this.game = game;
		backgrounds = game.AssetProvider.GraphicLoader.LoadAllBackgroundGraphics();
		clouds = game.AssetProvider.GraphicLoader.LoadAllCloudGraphics();
		skyGradients = game.AssetProvider.GraphicLoader.LoadSkyGradients();
    }

	public override void ScreenPushed(Game game, Screen screen)
	{
		base.ScreenPushed(game, screen);

		if (!screen.Transparent)
		{
			images.ForEach(image => image.Visible = false);
			skyGradient.ForEach(g => g.Visible = false);
			mapNameText!.Visible = false;
		}

		mouseDown = false;
		game.Pause();
	}

	public override void ScreenPopped(Game game, Screen screen)
	{
		if (!screen.Transparent)
		{
			SetLayout();
            images.ForEach(image => image.Visible = true);
			skyGradient.ForEach(g => g.Visible = true);
            mapNameText!.Visible = true;
        }

		base.ScreenPopped(game, screen);

		game.Resume();
	}

    private void SetLayout()
    {
		// TODO: For some reason the palette is not exactly the same as on the Atari ST. It is brighter there.
        game!.SetLayout(Layout.Map3D, palette);
    }

	private void ShowMapName()
	{
		var name = game!.AssetProvider.TextLoader.FromString(map!.Name).GetTextBlock(0);

        mapNameText ??= game!.TextManager.Create(name, Game.VirtualScreenWidth, 15, TextManager.TransparentPaper, palette);
        mapNameText.ShowInArea(OffsetX, OffsetY - mapNameText.LineHeight - 3, ViewWidth, ViewHeight, 100, TextAlignment.Center);
    }

    public override void Open(Game game, Action? closeAction)
	{
        buttonLayout = ButtonLayout.Movement;

        base.Open(game, closeAction);

		currentTicks = 0;
		lastMoveTicks = 0;
		lastTurnTicks = 0;
		mouseDown = false;

		SetLayout();
		LoadMap(game.State.MapIndex);
        ShowMapName();
        AfterMove();

		game.Time.MinuteChanged += MinuteChanged;
		game.CanSeeChanged += CanSeeChanged;
	}

	protected override void ButtonClicked(int index)
	{
		if (buttonLayout == ButtonLayout.Movement)
		{
			if (index == 4)
			{
				game!.Time.Tick();
				return;
			}

			int moveX = index % 3 - 1;
			int moveY = index / 3 - 1;

			void Move()
			{
				bool left = moveX < 0;
				bool right = moveX > 0;
				bool forward = moveY < 0;
				bool backward = moveY > 0;
				CheckMove(forward, backward, left, right, false, false);
			}

			void Turn(int dist)
			{
				bool turnLeft = dist < 0;
				bool turnRight = dist > 0;
				CheckMove(false, false, false, false, turnLeft, turnRight);
			}

			if ((moveX == 0) != (moveY == 0)) // move up, right, down or left
				Move();
			else if (moveY == -1) // turn left or right
				Turn(moveX);
			else // rotate left or right
				Rotate(moveX > 0);
		}
		else // Actions
		{
			if (index == 0) // eye
			{
                // TODO: NPCs

                var playerPosition = game!.State.PartyPosition;
                var forwardPosition = playerPosition + game.State.PartyDirection.Offset();

				if (forwardPosition.X >= 0 && forwardPosition.X < map!.Width &&
					forwardPosition.Y >= 0 && forwardPosition.Y < map.Height &&
					map.Tiles[forwardPosition.X + forwardPosition.Y * map.Width].Event != 0)
				{
                    game.EventHandler.HandleEvent(EventTrigger.Eye, Event.CreateEvent(map.Events[map.Tiles[forwardPosition.X + forwardPosition.Y * map.Width].Event - 1]), map);
                }
            }
			else if (index == 1) // ear
            {
                // TODO
            }
            else if (index == 2) // mouth
            {
                var character = characters.FirstOrDefault(character => character.Position == game!.State.PartyPosition);

				if (character != null && character.Type == MapCharacterType.Person)
				{
                    game!.State.CurrentConversationCharacterIndex = character.CharacterIndex;
					game.ScreenHandler.PushScreen(ScreenType.Conversation);
					return;
                }
            }
            else if (index == 4) // use magic
            {
                // TODO
            }
            else if (index == 5) // camp
            {
                // TODO
            }
            else if (index == 6) // map
            {
				// TODO
                // game!.ScreenHandler.PushScreen(ScreenType.Map);
            }
            else if (index == 7) // party positions
            {
                // TODO
            }
            else if (index == 8) // options
            {
                // TODO
            }
        }
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		if (buttonLayout == ButtonLayout.Movement)
		{
			// Upper row
			buttonGrid.SetButton(0, ButtonType.TurnLeft);
			buttonGrid.SetButton(1, ButtonType.MoveForward);
			buttonGrid.SetButton(2, ButtonType.TurnRight);
			// Middle row
			buttonGrid.SetButton(3, ButtonType.StrafeLeft);
			buttonGrid.EnableButton(3, true);
			buttonGrid.SetButton(4, ButtonType.Sleep);
			buttonGrid.SetButton(5, ButtonType.StrafeRight);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.RotateLeft);
			buttonGrid.SetButton(7, ButtonType.MoveBackward);
			buttonGrid.SetButton(8, ButtonType.RotateRight);
		}
		else // Actions
		{
			// Upper row
			buttonGrid.SetButton(0, ButtonType.Eye);
			buttonGrid.SetButton(1, ButtonType.Ear);
			buttonGrid.SetButton(2, ButtonType.Mouth);
			// Middle row
			buttonGrid.SetButton(3, ButtonType.UseTransport);
			buttonGrid.EnableButton(3, false);
			buttonGrid.SetButton(4, ButtonType.UseMagic);
			buttonGrid.SetButton(5, ButtonType.Camp);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.Map);
			buttonGrid.SetButton(7, ButtonType.PartyPositions);
			buttonGrid.SetButton(8, ButtonType.Disk);
		}
	}

	public override void Close(Game game)
	{
		mouseDown = false;
		game.Time.MinuteChanged -= MinuteChanged;
		game.CanSeeChanged -= CanSeeChanged;
		ClearView();
		skyGradient.ForEach(g => g.Visible = false);
		skyGradient.Clear();
		characters.Clear();
		mapNameText!.Delete();

		base.Close(game);
	}

	private void CanSeeChanged(bool canSee)
	{
		if (game!.Paused)
			return;

		UpdateLight();

		if (map!.Flags.HasFlag(MapFlags.City))
			UpdateSky(canSee);
	}

	private void MinuteChanged()
	{
		if (game!.Paused)
			return;

		var lightMode = map!.Flags.GetLightMode();

		if (lightMode != LightMode.Static)
			UpdateLight();

		if (map.Flags.HasFlag(MapFlags.City) && game!.CanSee())
			UpdateSky(true);

		if (characters.Count != 0) // TODO: check for active ones only (or maybe remove inactive ones)
		{
			foreach (var character in characters)
			{
				character.Update(game!);
			}

			var offsets = PerspectiveMappings[game!.State.PartyDirection];
			var playerPosition = game!.State.PartyPosition;

			for (int i = 0; i < 10; i++)
			{
				var offset = offsets[(PerspectiveLocation)i];
				int x = playerPosition.X + offset.X;
				int y = playerPosition.Y + offset.Y;

				if (characters.Any(character => character.Position.X == x && character.Position.Y == y))
				{
					UpdateView();
					break;
				}
			}
		}
	}

	public override void Update(Game game, long elapsedTicks)
	{
		if (elapsedTicks == 0)
			return;

		currentTicks += elapsedTicks;
		long animationFrame = currentTicks / AnimationTicksPerFrame;

		if (animationFrame != lastAnimationFrame)
		{
			lastAnimationFrame = animationFrame;

			foreach (var image in images)
				image.CurrentFrameIndex++;
		}

		if (game.InputEnabled && !game.Paused && currentTicks >= lastMoveTicks + TicksPerStep)
			CheckMove();
	}

	private void AfterMove()
	{
		// TODO

		var playerPosition = game!.State.PartyPosition;
		UpdateView();

		game.Time.Moved3D();

		// Check for events
		var eventIndex = map!.Tiles[playerPosition.X + playerPosition.Y * map.Width].Event;

		if (eventIndex != 0)
		{
			var mapEvent = Event.CreateEvent(map.Events[eventIndex - 1]);

			if (mapEvent is IPlaceEvent)
			{
				game.State.ResetPartyPosition();
                UpdateView();
            }

			game.EventHandler.HandleEvent(EventTrigger.Move, mapEvent, map);
		}
	}

	private bool CanMoveTo(int x, int y, bool player, int collisionClass)
	{
		if (x < 0 || y < 0 || x >= map!.Width || y >= map.Height)
			return false;

		if (!player)
		{
			var testPosition = new Position(x, y);

			if (characters.Any(character => character.Position == testPosition))
				return false;
		}

		var tileFlags = GetTileFlags(x, y);

		if (tileFlags.HasFlag(LabTileFlags.BlockAllMovement))
			return false;

		if (!tileFlags.HasFlag((LabTileFlags)(1 << (8 + collisionClass))))
			return false;

		return true;
	}

	private void UpdateLight()
	{
		// TODO
	}

	private void UpdateSky(bool canSee)
	{
		if (!canSee)
		{
			skyGradient.ForEach(g => g.Visible = false);
			skyGradient.Clear();
			return;
		}

		var dayTime = game!.State.Hour.HourToDayTime();
		var gradient = skyGradients[dayTime];
		var skyColor = game.AssetProvider.PaletteLoader.LoadPalette(palette).GetColorAt(SkyTransparentColorIndex, 0);

		if (skyGradient.Count == 0)
		{
			void CreateSkyLine(int y, Color color)
			{
				var skyLine = game!.GetRenderLayer(Layer.Map3D).ColoredRectFactory!.Create();
				skyLine.Color = color;
				skyLine.Position = new(OffsetX, OffsetY + y);
				skyLine.Size = new(ViewWidth, 1);
				skyLine.DisplayLayer = 0;
				skyLine.Visible = true;				
				skyGradient.Add(skyLine);
			}

			for (int y = 0; y < gradient.Length - 1; y++)
				CreateSkyLine(y, gradient[y]);
			
			CreateSkyLine(gradient.Length - 1, skyColor);
		}
	}

	private LabTileFlags GetTileFlags(int x, int y)
	{
		var tile = map!.Tiles[x + y * map.Width];

		if (tile.LabTileIndex == 0)
			return LabTileFlags.None;

		var labTile = map.LabTiles[tile.LabTileIndex - 1];

		return labTile.Flags;
	}

	private void CheckMove(bool forward, bool backward, bool left, bool right, bool turnLeft, bool turnRight)
	{
		switch (game!.State.PartyDirection)
		{
			case Direction.North:
				if (forward && !backward)
					Move(0, -1);
				else if (backward && !forward)
					Move(0, 1);
				else if (left && !right)
					Move(-1, 0);
				else if (right && !left)
					Move(1, 0);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.West);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.East);
				break;
			case Direction.East:
				if (forward && !backward)
					Move(1, 0);
				else if (backward && !forward)
					Move(-1, 0);
				else if (left && !right)
					Move(0, -1);
				else if (right && !left)
					Move(0, 1);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.North);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.South);
				break;
			case Direction.South:
				if (forward && !backward)
					Move(0, 1);
				else if (backward && !forward)
					Move(0, -1);
				else if (left && !right)
					Move(1, 0);
				else if (right && !left)
					Move(-1, 0);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.East);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.West);
				break;
			case Direction.West:
				if (forward && !backward)
					Move(-1, 0);
				else if (backward && !forward)
					Move(1, 0);
				else if (left && !right)
					Move(0, 1);
				else if (right && !left)
					Move(0, -1);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.South);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.North);
				break;
		}
	}

	private void CheckMove()
	{
		bool left = game!.IsKeyDown('A');
		bool right = game.IsKeyDown('D');
		bool forward = game.IsKeyDown(Key.Up) || game.IsKeyDown('W');
		bool backward = game.IsKeyDown(Key.Down) || game.IsKeyDown('S');
		bool turnLeft = game.IsKeyDown(Key.Left) || game.IsKeyDown('Q');
		bool turnRight = game.IsKeyDown(Key.Right) || game.IsKeyDown('E');

		if (mouseDown && game.InputEnabled && !game.Paused)
		{
			switch (game!.Cursor.CursorType)
			{
				case CursorType.ArrowForward3D:
					forward = true;
					break;
				case CursorType.ArrowBackward3D:
					backward = true;
					break;
				case CursorType.ArrowLeft3D:
					left = true;
					break;
				case CursorType.ArrowRight3D:
					right = true;
					break;
				case CursorType.ArrowTurnLeft3D:
					turnLeft = true;
					break;
				case CursorType.ArrowTurnRight3D:
					turnRight = true;
					break;
				case CursorType.FullTurnLeft:
					// TODO
					break;
				case CursorType.FullTurnRight:
					// TODO
					break;
			}
		}

		if (buttonLayout == ButtonLayout.Movement)
		{
			if (!left)
				left = game.IsKeyDown(Key.Keypad4);
			if (!right)
				right = game.IsKeyDown(Key.Keypad6);
			if (!forward)
				forward = game.IsKeyDown(Key.Keypad8);
			if (!backward)
				backward = game.IsKeyDown(Key.Keypad2);
			if (!turnLeft)
				turnLeft = game.IsKeyDown(Key.Keypad7);
			if (!turnRight)
				turnRight = game.IsKeyDown(Key.Keypad9);
			// TODO
			/*if (!fullTurnLeft)
				fullTurnLeft = game.IsKeyDown(Key.Keypad1);
			if (!fullTurnRight)
				fullTurnRight = game.IsKeyDown(Key.Keypad3);*/
		}

		CheckMove(forward, backward, left, right, turnLeft, turnRight);
	}

	public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (key >= Key.F1 && key <= Key.F6)
        {
            game!.State.CurrentInventoryIndex = key - Key.F1;
            game.ScreenHandler.PushScreen(ScreenType.Inventory);
			return;
        }

        CheckMove();
	}

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Right)
		{
			if (ButtonGrid.Area.Contains(position))
			{
				buttonLayout = (ButtonLayout)(1 - (int)buttonLayout); // toggle
				RequestButtonSetup();
			}
		}
		else
		{
			mouseDown = true;
			var mapArea = new Rect(OffsetX, OffsetY, ViewWidth, ViewHeight);

			if (mapArea.Contains(position))
			{
				if (game!.Cursor.CursorType == CursorType.Zzz)
					game.Time.Tick();
				else if (game.Cursor.CursorType >= CursorType.ArrowUp2D && game.Cursor.CursorType <= CursorType.ArrowDownLeft2D)
					CheckMove();

				return;
			}

			base.MouseDown(position, buttons, keyModifiers);
        }
	}

	public override void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		mouseDown = false;

		base.MouseUp(position, buttons, keyModifiers);
	}

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		base.MouseMove(position, buttons);

		var mapArea = new Rect(OffsetX, OffsetY, ViewWidth, ViewHeight);

		if (mapArea.Contains(position))
		{
			int relativeX = position.X - mapArea.Left;
			int relativeY = position.Y - mapArea.Top;
			bool left = relativeX < mapArea.Size.Width / 4;
			bool right = relativeX >= mapArea.Size.Width * 3 / 4;
			bool up = relativeY < mapArea.Size.Height / 4;
			bool down = relativeY >= mapArea.Size.Height * 3 / 4;

			var lastCursor = game!.Cursor.CursorType;

			if (up)
			{
				if (left)
					game.Cursor.CursorType = CursorType.ArrowTurnLeft3D;
				else if (right)
					game.Cursor.CursorType = CursorType.ArrowTurnRight3D;
				else
					game.Cursor.CursorType = CursorType.ArrowForward3D;
			}
			else if (down)
			{
				if (left)
					game.Cursor.CursorType = CursorType.FullTurnLeft;
				else if (right)
					game.Cursor.CursorType = CursorType.FullTurnRight;
				else
					game.Cursor.CursorType = CursorType.ArrowBackward3D;
			}
			else
			{
				if (left)
					game.Cursor.CursorType = CursorType.ArrowLeft3D;
				else if (right)
					game.Cursor.CursorType = CursorType.ArrowRight3D;
				else
					game.Cursor.CursorType = CursorType.Zzz;
			}

			if (mouseDown && lastCursor != game.Cursor.CursorType)
				CheckMove();
		}
		else if (game!.Cursor.CursorType != CursorType.Sword)
		{
			game!.Cursor.CursorType = CursorType.Sword;
		}
	}

	private void Move(int x, int y)
	{
		if (currentTicks - lastMoveTicks < TicksPerStep)
			return;

		lastMoveTicks = currentTicks;
		int targetX = game!.State.PartyPosition.X + x;
		int targetY = game.State.PartyPosition.Y + y;

		if (CanMoveTo(targetX, targetY, true, 0))
		{
			game!.State.SetPartyPosition(targetX, targetY);
			AfterMove();
		}
		else
		{
			// TODO: ouch
		}
	}

	private void TurnTo(Direction newDirection)
	{
		if (currentTicks - lastTurnTicks < TicksPerTurn)
			return;

		lastTurnTicks = currentTicks;
		game!.State.PartyDirection = newDirection;
		UpdateView();
	}

	private void Rotate(bool right)
	{
		// TODO
	}

	private BlockFacing FacingByRelativeOffset(Position offset)
	{
		switch (game!.State.PartyDirection)
		{
			case Direction.North:
				if (offset.X < 0)
					return BlockFacing.LeftOfPlayer;
				else if (offset.X > 0) 
					return BlockFacing.RightOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.East:
				if (offset.Y < 0)
					return BlockFacing.LeftOfPlayer;
				else if (offset.Y > 0)
					return BlockFacing.RightOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.South:
				if (offset.X < 0)
					return BlockFacing.RightOfPlayer;
				else if (offset.X > 0)
					return BlockFacing.LeftOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.West:
				if (offset.Y < 0)
					return BlockFacing.RightOfPlayer;
				else if (offset.Y > 0)
					return BlockFacing.LeftOfPlayer;
				return BlockFacing.FacingPlayer;
			default:
				return BlockFacing.FacingPlayer;
		}
	}

	private void UpdateView()
	{
		var playerPosition = game!.State.PartyPosition;

		ClearView();

		var offsets = PerspectiveMappings[game.State.PartyDirection];
		var layer = game.GetRenderLayer(Layer.Map3D);
		var textureAtlas = layer.Config.Texture!;
		byte displayLayer = 40;

		var floor = backgrounds[labData!.FloorIndex];
		var floorSprite = layer.SpriteFactory!.CreateAnimated();
		floorSprite.Size = new(floor.Width, floor.Height);
		floorSprite.Position = new(OffsetX, OffsetY + ViewHeight - floor.Height);
		floorSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetBackgroundGraphicIndex(labData.FloorIndex));
		floorSprite.Opaque = true;
		floorSprite.DisplayLayer = 5;
		floorSprite.PaletteIndex = palette;
		floorSprite.Visible = true;
		images.Add(floorSprite);

		var hasSky = map!.Flags.HasFlag(MapFlags.City);
		var dayTime = game.State.Hour.HourToDayTime();

		if (hasSky && (dayTime == DayTime.Day || dayTime == DayTime.Dusk))
		{
			// Clouds
			var cloud = clouds[labData!.CeilingIndex];
			var cloudSprite = layer.SpriteFactory!.CreateAnimated();
			cloudSprite.Size = new(cloud.Width, cloud.Height);
			cloudSprite.Position = new(OffsetX, OffsetY);
			cloudSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetCloudGraphicIndex(labData.CeilingIndex));
			cloudSprite.TransparentColorIndex = SkyTransparentColorIndex;
			cloudSprite.DisplayLayer = 12;
			cloudSprite.PaletteIndex = palette;
			cloudSprite.Visible = true;
			images.Add(cloudSprite);
		}
		else
		{
			// Normal sky or ceiling
			var ceiling = backgrounds[labData!.CeilingIndex];
			var ceilingSprite = layer.SpriteFactory!.CreateAnimated();
			ceilingSprite.Size = new(ceiling.Width, ceiling.Height);
			ceilingSprite.Position = new(OffsetX, OffsetY);
			ceilingSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetBackgroundGraphicIndex(labData.CeilingIndex));
			ceilingSprite.Opaque = !hasSky;
			ceilingSprite.TransparentColorIndex = (byte)(hasSky ? SkyTransparentColorIndex : 0);
			ceilingSprite.DisplayLayer = 10;
			ceilingSprite.PaletteIndex = palette;
			ceilingSprite.Visible = true;
			images.Add(ceilingSprite);
		}

		for (int i = 0; i < 14; i++)
		{
			var perspectiveLocation = (PerspectiveLocation)i;
			var offset = offsets[perspectiveLocation];
			int x = playerPosition.X + offset.X;
			int y = playerPosition.Y + offset.Y;
			var tile = map!.Tiles[x + y * map.Width];

			if (tile.LabTileIndex == 0)
				continue;

			var labTile = map!.LabTiles[tile.LabTileIndex - 1];

			void DrawBlock(ILabBlock labBlock, int? customRenderX = null)
			{
				if (labBlock.Type != LabBlockType.Wall && i > 11)
					return;

				if (i > 11)
				{
					displayLayer = (byte)(20 + (i % 12) * 10);
					perspectiveLocation = (PerspectiveLocation)((int)perspectiveLocation % 12);
				}

				var facing = labBlock.Type == LabBlockType.Overlay ? FacingByRelativeOffset(offset) : BlockFacing.FacingPlayer;
				byte displayPlayerAdd = (byte)(facing == BlockFacing.FacingPlayer ? 10 : 5);

				if (facing != BlockFacing.FacingPlayer)
					AddBlockSprite(facing);

				AddBlockSprite(BlockFacing.FacingPlayer);

				void AddBlockSprite(BlockFacing facing)
				{
					var perspective = labBlock.Perspectives.FirstOrDefault(p => p.Location == perspectiveLocation && p.Facing == facing);

					if (perspective.Frames == null)
						return;

					if (customRenderX == OffsetX + ViewWidth)
						customRenderX -= perspective.Frames[0].Width / 2;

					if (perspective.SpecialRenderPosition != null)
					{
						int graphicIndex = game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing);
						var textureOffset = textureAtlas.GetOffset(graphicIndex);
						var blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = 1;
						blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureOffset;
						blockSprite.Position = new(OffsetX + perspective.RenderPosition.X, OffsetY + perspective.RenderPosition.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);

						displayLayer += (byte)(displayPlayerAdd / 2);

						blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = perspective.Frames.Length - 1;
						blockSprite.Size = new Size(perspective.Frames[1].Width, perspective.Frames[1].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = new(textureOffset.X + perspective.Frames[0].Width, textureOffset.Y);
						blockSprite.Position = new(OffsetX + perspective.SpecialRenderPosition.Value.X, OffsetY + perspective.SpecialRenderPosition.Value.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);
					}
					else
					{
						var blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = perspective.Frames.Length;
						blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing));
						blockSprite.Position = new(customRenderX ?? (OffsetX + perspective.RenderPosition.X), OffsetY + perspective.RenderPosition.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);
					}

					displayLayer += displayPlayerAdd;
				}
			}

			var primary = labData!.LabBlocks[labTile.PrimaryLabBlockIndex - 1];

			if (primary.Type == LabBlockType.Overlay && labTile.SecondaryLabBlockIndex != 0)
			{
				// Draw underlay for overlays
				DrawBlock(labData!.LabBlocks[labTile.SecondaryLabBlockIndex - 1]);
			}

			// Draw underlay or overlay
			if (labTile.PrimaryLabBlockIndex != 1) // 1 seems to be a marker for free tiles
				DrawBlock(primary);

			var character = characters.FirstOrDefault(character => character.Position == new Position(x, y));

			if (character != null)
			{
				var objectBlock = labData!.LabBlocks[character.Icon - 1];
				int? customX = null;

				if ((int)perspectiveLocation % 3 == 0) // left row
					customX = OffsetX;
				else if ((int)perspectiveLocation % 3 == 1) // right row
					customX = OffsetX + ViewWidth;

				DrawBlock(objectBlock, customX);
			}
		}
	}

	private void ClearView()
	{
		images.ForEach(image => image.Visible = false);
		images.Clear();
	}

	private void LoadMap(int index)
	{
		map = game!.AssetProvider.MapLoader.LoadMap(index) as IMap3D; // TODO: catch exceptions
		labData = game!.AssetProvider.LabDataLoader.LoadLabData(map!.LabDataIndex);
		palette = game.PaletteIndexProvider.GetLabyrinthPaletteIndex(labData.PaletteIndex - 1);

		for (int i = 0; i < map.Characters.Length; i++)
		{
			var characterData = map.Characters[i];

			if (characterData.Index != 0 && characterData.Icon != 0)
			{
				characters.Add(new Character(map, i, map.CharacterPositions[i], game.State,
					(int x, int y, int collisionClass) => CanMoveTo(x, y, false, collisionClass)));
			}
		}

		var lightMode = map.Flags.GetLightMode();

		if (lightMode != LightMode.Static)
			UpdateLight();

		if (map.Flags.HasFlag(MapFlags.City) && game!.CanSee())
			UpdateSky(true);

		game.State.MapIndex = index;
		game.State.SetIsWorldMap(false);
		game.State.TravelType = TravelType.Walk;
		game.Cursor.PaletteIndex = palette;
		RequestButtonGridPaletteUpdate();

    }
}
