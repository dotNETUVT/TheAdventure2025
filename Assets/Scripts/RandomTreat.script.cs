using TheAdventure.Scripting;
using System;
using TheAdventure;

public class RandomTreat : IScript
{
    DateTimeOffset _nextTreatTimestamp;

    public void Initialize()
    {
        _nextTreatTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(5, 9));
    }

    public void Execute(Engine engine)
    {
        if (_nextTreatTimestamp < DateTimeOffset.UtcNow)
        {
            _nextTreatTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(5, 9));
            var treatPosX = Random.Shared.Next(100, 640);
            var treatPosY = Random.Shared.Next(100, 400);
            engine.AddTreat(treatPosX, treatPosY, false);
        }
    }
}



// using TheAdventure.Scripting;
// using System;
// using TheAdventure;
// using TheAdventure.Models;


// public class RandomTreat : IScript
// {
//     private DateTimeOffset _nextTreatTimestamp;
//     private readonly List<(DateTimeOffset Expiry, TreatObject Treat)> _activeTreats = new();
//     private SpriteSheet _treatSpriteSheet;
//     private GameRenderer _renderer;

//     public void Initialize()
//     {
//         _nextTreatTimestamp = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(3, 8));
//         _treatSpriteSheet = SpriteSheet.Load(_renderer, "Treat.json", "Assets");
//     }

//     public void SetRenderer(GameRenderer renderer)
//     {
//         _renderer = renderer;
//     }
//     public void Execute(Engine engine)
//     {
//         if (_renderer == null) _renderer = engine.GetRenderer();
//         if (_treatSpriteSheet == null && _renderer != null)
//         {
//             _treatSpriteSheet = SpriteSheet.Load(_renderer, "Treat.json", "Assets");
//         }

//         var now = DateTimeOffset.UtcNow;

//         if (_nextTreatTimestamp < now)
//         {
//             _nextTreatTimestamp = now.AddSeconds(Random.Shared.Next(3, 8));
//             var treatPosX = Random.Shared.Next(100, 640);
//             var treatPosY = Random.Shared.Next(100, 400);
//             var treat = new TreatObject(_treatSpriteSheet, (treatPosX, treatPosY));
//             _activeTreats.Add((now.AddSeconds(5), treat));
//             engine.AddTreat(treat);
//         }

//         for (int i = _activeTreats.Count - 1; i >= 0; i--)
//         {
//             if (_activeTreats[i].Expiry < now || _activeTreats[i].Treat.IsCollected)
//             {
//                 engine.RemoveTreat(_activeTreats[i].Treat);
//                 _activeTreats.RemoveAt(i);
//             }
//         }
//     }
// }