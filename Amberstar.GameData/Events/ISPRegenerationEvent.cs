﻿namespace Amberstar.GameData.Events;

/// <summary>
/// Fills up spell points.
/// 
/// Optionally displays a text beforehand.
/// </summary>
public interface ISPRegenerationEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// 0 means "fill".
	/// </summary>
	byte Amount { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the regeneration.
	/// </summary>
	byte TextIndex { get; }

	bool Fill { get; }
}
