using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure
{
    public class GameLogic
    {
        private readonly Dictionary<int, GameObject> _gameObjects = new();
        private readonly Dictionary<string, TileSet> _loadedTileSets = new();
        private readonly Dictionary<int, Tile> _tileIdMap = new();
        private Level _currentLevel = new();

        private int _bombIds = 100;

        public void InitializeGame()
        {
            var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
            var level = JsonSerializer.Deserialize<Level>(levelContent);
            if (level == null)
            {
                throw new Exception("Failed to load level");
            }

            foreach (var tileSetRef in level.TileSets)
            {
                var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
                var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
                if (tileSet == null)
                {
                    throw new Exception("Failed to load tile set");
                }

                foreach (var tile in tileSet.Tiles)
                {
                    tile.TextureId = GameRenderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                    _tileIdMap.Add(tile.Id!.Value, tile);
                }

                _loadedTileSets.Add(tileSet.Name, tileSet);
            }

            _currentLevel = level;
        }

        public void ProcessFrame()
        {
        }

        public void RenderAllObjects(int timeSinceLastFrame, GameRenderer renderer)
        {
            List<int> itemsToRemove = new List<int>();
            foreach (var gameObject in GetRenderables())
            {
                if (gameObject.Update(timeSinceLastFrame))
                {
                    gameObject.Render(renderer);
                }
                else
                {
                    itemsToRemove.Add(gameObject.Id);
                }
            }
            foreach (var item in itemsToRemove)
            {
                if (_gameObjects.TryGetValue(item, out var obj) && obj is AnimatedGameObject animObj)
                {
                    animObj.OnDestroy?.Invoke(this);
                }
                _gameObjects.Remove(item);
            }
        }

        public void AddBomb(int x, int y)
        {
            AnimatedGameObject bomb = new AnimatedGameObject(
                Path.Combine("Assets", "BombExploding.png"),
                durationInSeconds: 2,
                id: _bombIds,
                numberOfFrames: 13,
                numberOfColumns: 13,
                numberOfRows: 1,
                x: x,
                y: y
            );
            bomb.OnDestroy = logic =>
            {
                float explosionRadius = 100f; 
                logic.TriggerBombExplosionChain(bomb, explosionRadius);
            };

            _gameObjects.Add(bomb.Id, bomb);
            ++_bombIds;
        }

        public void TriggerBombExplosionChain(AnimatedGameObject sourceBomb, float explosionRadius)
        {
            foreach (var obj in _gameObjects.Values)
            {
                if (obj is AnimatedGameObject otherBomb && otherBomb != sourceBomb)
                {
                    float dx = otherBomb.X - sourceBomb.X;
                    float dy = otherBomb.Y - sourceBomb.Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist <= explosionRadius)
                    {
                        otherBomb.ForceEndNow = true;
                    }
                }
            }
        }

        public void RenderTerrain(GameRenderer renderer)
        {
            foreach (var currentLayer in _currentLevel.Layers)
            {
                for (int i = 0; i < _currentLevel.Width; ++i)
                {
                    for (int j = 0; j < _currentLevel.Height; ++j)
                    {
                        int? dataIndex = j * currentLayer.Width + i;
                        if (dataIndex == null)
                            continue;

                        var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                        if (currentTileId == null)
                            continue;

                        var currentTile = _tileIdMap[currentTileId.Value];
                        var tileWidth = currentTile.ImageWidth ?? 0;
                        var tileHeight = currentTile.ImageHeight ?? 0;

                        var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                        var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                        renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                    }
                }
            }
        }

        public IEnumerable<RenderableGameObject> GetRenderables()
        {
            foreach (var gameObject in _gameObjects.Values)
            {
                if (gameObject is RenderableGameObject renderableGameObject)
                {
                    yield return renderableGameObject;
                }
            }
        }
    }
}
