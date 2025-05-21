namespace TheAdventure2025.Models.Data
{
    public enum ItemType { Weapon, Potion, Tool }
    public enum Rarity { Common, Rare, Legendary }

    public class Item
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public Rarity Rarity { get; set; }

        public Item(string name, ItemType type, Rarity rarity)
        {
            Name = name;
            Type = type;
            Rarity = rarity;
        }

        public override string ToString()
        {
            return $"{Rarity} {Type}: {Name}";
        }
    }
}
