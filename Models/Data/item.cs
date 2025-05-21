namespace TheAdventure.Models.Data
{
    public enum ItemType
    {
        Weapon,
        Potion,
        Tool,
        Quest
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public class Item
    {
        public string Name { get; }
        public ItemType Type { get; }
        public Rarity Rarity { get; }

        public Item(string name, ItemType type, Rarity rarity)
        {
            Name = name;
            Type = type;
            Rarity = rarity;
        }

        public override string ToString()
        {
            return $"{Name} ({Type}, {Rarity})";
        }
    }
}
