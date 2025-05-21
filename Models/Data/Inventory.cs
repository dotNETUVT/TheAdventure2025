using System.Collections.Generic;
using System.Linq;

namespace TheAdventure.Models.Data
{
    public class Inventory
    {
        private readonly List<Item> _items = new();

        public void AddItem(Item item) => _items.Add(item);

        public bool RemoveItem(string name)
        {
            var item = _items.FirstOrDefault(i => i.Name == name);
            if (item != null)
            {
                _items.Remove(item);
                return true;
            }
            return false;
        }

        public List<Item> FilterByType(ItemType type) => _items.Where(i => i.Type == type).ToList();

        public List<Item> FilterByRarity(Rarity rarity) => _items.Where(i => i.Rarity == rarity).ToList();

        public List<Item> GetAllItems() => new(_items);
    }
}