using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure2025;

namespace TheAdventure;

public class GameLogic
{
    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private int _bombIds = 100;

    public void InitializeGame()
    {
        _player = new(1000);

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
            _gameObjects.Remove(item);
        }

        _player?.Render(renderer);
    }

    public void UpdatePlayerPosition(double up, double down, double left, double right, int timeSinceLastUpdateInMs)
    {
        if (PauseManager.IsPaused)
            return;
        _player?.UpdatePosition(up, down, left, right, timeSinceLastUpdateInMs);
    }

    public (int X, int Y) GetPlayerPosition()
    {
        if (_player == null)
        {
            return (0, 0);
        }

        return (_player.X, _player.Y);
    }

    public void AddBomb(int x, int y)
    {
        AnimatedGameObject bomb =
            new AnimatedGameObject(Path.Combine("Assets", "BombExploding.png"), 2, _bombIds, 13, 13, 1, x, y);
        _gameObjects.Add(bomb.Id, bomb);
        ++_bombIds;
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
                    {
                        continue;
                    }

                    var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                    if (currentTileId == null)
                    {
                        continue;
                    }

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