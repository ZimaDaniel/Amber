﻿namespace Amberstar.GameData;

public record MonsterSpell(SpellSchool School, byte SpellIndex);

public interface IMonster : IBattleCharacter
{
    byte Morale { get; init; }
    MonsterFlags MonsterFlags { get; init; }
    MonsterElementalFlags ElementalFlags { get; init; }
    /// <summary>
    /// Spells are randomly picked from this list.
    /// A spell can exist multiple times to increase it's chance to be picked.
    /// </summary>
    MonsterSpell[] Spells { get; init; }
}