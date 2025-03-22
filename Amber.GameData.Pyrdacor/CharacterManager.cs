namespace Amber.GameData.Pyrdacor
{
    internal class CharacterManager : ICharacterManager
    {
        readonly Lazy<Dictionary<uint, Character>> characters;
        readonly Lazy<Dictionary<uint, Monster>> monsters;        
        readonly Lazy<Dictionary<uint, MonsterGroup>> monsterGroups;

        public CharacterManager(Func<Dictionary<uint, Character>> characterProvider,
            Func<Dictionary<uint, Monster>> monsterProvider,
            Func<Dictionary<uint, MonsterGroup>> monsterGroupProvider)
        {
            characters = new Lazy<Dictionary<uint, Character>>(characterProvider);
            monsters = new Lazy<Dictionary<uint, Monster>>(monsterProvider);
            monsterGroups = new Lazy<Dictionary<uint, MonsterGroup>>(monsterGroupProvider);
        }

        public Monster? GetMonster(uint index) => index == 0 || !monsters.Value.ContainsKey(index) ? null : monsters.Value[index];

        public Character? GetCharacter(uint index) => index == 0 || !characters.Value.ContainsKey(index) ? null : characters.Value[index];

        public MonsterGroup? GetMonsterGroup(uint index) => index == 0 || !monsterGroups.Value.ContainsKey(index) ? null : monsterGroups.Value[index];

        public IReadOnlyList<Character> Characters => characters.Value.Values.ToList().AsReadOnly();
		public IReadOnlyList<Monster> Monsters => monsters.Value.Values.ToList().AsReadOnly();
        public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups.Value.AsReadOnly();
    }
}
