﻿using Amber.Assets.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class ItemLoader : IItemLoader
	{
		public IItem LoadItem(IAsset asset)
        {
            var reader = asset.GetReader();

            return ReadItem( reader);
		}

        public IItem ReadItem(IDataReader reader)
        {
            var graphicIndex = (ItemGraphic)reader.ReadByte();
            var itemType = (ItemType)reader.ReadByte();
            var usedAmmoType = (AmmoType)reader.ReadByte();
            var genders = (GenderFlags)reader.ReadByte();
            var numHands = reader.ReadByte();
            var numFingers = reader.ReadByte();
            var hitPoints = reader.ReadByte();
            var spellPoints = reader.ReadByte();
            var attribute = reader.ReadByte();
            var attributeValue = reader.ReadByte();
            var skill = reader.ReadByte();
            var skillValue = reader.ReadByte();
            var spellSchool = (SpellSchool)reader.ReadByte();
            var spellIndex = reader.ReadByte();
            var spellCharges = reader.ReadByte();
            var ammoType = (AmmoType)reader.ReadByte();
            var defense = reader.ReadByte();
            var damage = reader.ReadByte();
            var equipmentSlot = (EquipmentSlot)reader.ReadByte();
            var magicWeaponBonus = reader.ReadByte();
            var magicArmorBonus = reader.ReadByte();
            var specialIndex = reader.ReadByte();
            var initialCharges = reader.ReadByte();
            var maxCharges = reader.ReadByte();
            var itemFlags = (ItemFlags)reader.ReadByte();
            var malusSkill1 = reader.ReadByte();
            var malusSkill2 = reader.ReadByte();
            var malus1 = reader.ReadByte();
            var malus2 = reader.ReadByte();
            var textIndex = reader.ReadByte();
            var usableClasses = (ClassFlags)reader.ReadWord();
            var buyPrice = reader.ReadWord();
            var weight = reader.ReadWord();
            var index = reader.ReadWord();
            var nameIndex = reader.ReadWord();

            return new Item()
            {
                Index = index,
                Type = itemType,
                GraphicIndex = graphicIndex,
                UsedAmmoType = usedAmmoType,
                Genders = genders,
                Hands = numHands,
                Fingers = numFingers,
                HitPoints = hitPoints,
                SpellPoints = spellPoints,
                Attribute = attribute == 0 ? null : (Attribute?)(attribute - 1),
                AttributeValue = attributeValue,
                Skill = skill == 0 ? null : (Skill?)(skill - 1),
                SkillValue = skillValue,
                SpellSchool = spellSchool,
                SpellIndex = spellIndex,
                SpellCharges = spellCharges,
                AmmoType = ammoType,
                Defense = defense,
                Damage = damage,
                EquipmentSlot = equipmentSlot == 0 ? null : (EquipmentSlot?)(equipmentSlot - 1),
                MagicWeaponBonus = magicWeaponBonus,
                MagicArmorBonus = magicArmorBonus,
                SpecialIndex = specialIndex,
                InitialCharges = initialCharges,
                MaxCharges = maxCharges,
                Flags = itemFlags,
                MalusSkill1 = malusSkill1 == 0 ? null : (Skill?)(malusSkill1 - 1),
                MalusSkill2 = malusSkill2 == 0 ? null : (Skill?)(malusSkill2 - 1),
                Malus1 = malus1,
                Malus2 = malus2,
                TextIndex = textIndex,
                UsableClasses = usableClasses,
                BuyPrice = buyPrice,
                Weight = weight,
                NameIndex = nameIndex
            };
        }
    }
}