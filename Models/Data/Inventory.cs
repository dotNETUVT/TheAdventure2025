using System.Collections.Generic;
using System.Linq;

namespace TheAdventure2025.Models.Data
{
    public class Inventory
    {
        private List<Item> items = new();

        public void AddItem(Item item) => items.Add(item);

        public void RemoveItem(Item item) => items.Remove(item);

        public List<Item> FilterByType(ItemType type)
            => items.Where(i => i.Type == type).ToList();

        public List<Item> FilterByRarity(Rarity rarity)
            => items.Where(i => i.Rarity == rarity).ToList();

        public List<Item> GetAllItems() => items;
    }
}
