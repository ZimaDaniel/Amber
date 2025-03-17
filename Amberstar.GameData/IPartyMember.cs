﻿using Amber.Assets.Common;

namespace Amberstar.GameData;

public interface IPartyMember : IBattleCharacter
{
    ClassFlags PossibleClasses { get; init; }
    byte DefaultBattlePosition { get; init; }
    word AttackPerRoundLevel { get; init; }
    word HitPointsPerLevel { get; init; }
    word SpellPointsPerLevel { get; init; }
    word SpellLearningPointsPerLevel { get; init; }
    word TrainingPointsPerLevel { get; init; }
    dword ExperiencePoints { get; set; }
    SpellSchoolFlags LearnedSpellSchools { get; set; }
    dword LearnedWhiteSpells { get; set; }
    dword LearnedGraySpells { get; set; }
    dword LearnedBlackSpells { get; set; }
    dword LearnedSpecialSpells { get; set; }
    dword TotalWeight { get; } // We should calculate it and only use it for display etc
    IConversationData ConversationData { get; init; }
    public CharacterValue Age => ConversationData.Age;
    public IGraphic? Portrait => ConversationData.Portrait;

    /*
     * Level Up:
     * 
     * - Increase level by 1
     * - If APRPerLvl == 0, don't change APR
     * - Otherwise APR = level / APRPerLvl
     * - HP += (TotalSTA / 10) + HPPerLvl
     * - If magic:
     * -  SP += (TotalINT / 20) + SPPerLvl
     * -  SLP += (TotalINT / 20) + SLPPerLvl
     */
}
