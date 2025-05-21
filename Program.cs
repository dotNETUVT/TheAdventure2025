using Silk.NET.SDL;
using System;
using Thread = System.Threading.Thread;
using TheAdventure.Models.Data; // ✅ Correct namespace for Inventory & Item

namespace TheAdventure
{
    public static class Program
    {
        public static void Main()
        {
            // ✅ Inventory demo — BEFORE game loop
            var inventory = new Inventory();
            inventory.AddItem(new Item("Sword", ItemType.Weapon, Rarity.Common));
            inventory.AddItem(new Item("Magic Wand", ItemType.Weapon, Rarity.Rare));
            inventory.AddItem(new Item("Health Potion", ItemType.Potion, Rarity.Common));
            inventory.AddItem(new Item("Ancient Hammer", ItemType.Tool, Rarity.Legendary));

            Console.WriteLine("🧰 All Inventory Items:");
            foreach (var item in inventory.GetAllItems())
                Console.WriteLine(item);

            Console.WriteLine("\n🟣 Rare Items:");
            foreach (var item in inventory.FilterByRarity(Rarity.Rare))
                Console.WriteLine(item);

            Console.WriteLine("\n⚔️  Weapons:");
            foreach (var item in inventory.FilterByType(ItemType.Weapon))
                Console.WriteLine(item);

            // ✅ Game engine starts here
            var sdl = new Sdl(new SdlContext());

            var sdlInitResult = sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                                         Sdl.InitGamecontroller |
                                         Sdl.InitJoystick);
            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException("Failed to initialize SDL.");
            }

            using (var gameWindow = new GameWindow(sdl))
            {
                var input = new Input(sdl);
                var gameRenderer = new GameRenderer(sdl, gameWindow);
                var engine = new Engine(gameRenderer, input);

                engine.SetupWorld();

                bool quit = false;
                while (!quit)
                {
                    quit = input.ProcessInput();
                    if (quit) break;

                    engine.ProcessFrame();
                    engine.RenderFrame();

                    Thread.Sleep(13);
                }
            }

            sdl.Quit();
        }
    }
}
