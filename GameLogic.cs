using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class GameLogic
{
    private readonly GameRenderer _renderer;

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private int _bombIds = 100;
    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public GameLogic(GameRenderer renderer)
    {
        _renderer = renderer;
    }

    public void InitializeGame()
    {
        _player = new(_renderer);

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
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }

            _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
        }

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));

        _currentLevel = level;
    }

    public void ProcessFrame()
    {
    }
    
    public void RenderFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;
        
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();
        
        _renderer.CameraLookAt(_player!.X, _player!.Y);
        
        RenderTerrain();
        RenderAllObjects(msSinceLastFrame);

        //Use the RenderStats to display the statistics
       _renderer.RenderStats(_player);

        _renderer.PresentFrame();
    }

    public void RenderAllObjects(double msSinceLastFrame)
    {
        List<int> itemsToRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            if (gameObject.Update(msSinceLastFrame))
            {
                gameObject.Render(_renderer);
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

        _player?.Render(_renderer);
    }

    public void UpdatePlayerPosition(double up, double down, double left, double right, int timeSinceLastUpdateInMs)
    {
        _player?.UpdatePosition(up, down, left, right, timeSinceLastUpdateInMs);
    }

    public void AddBomb(int screenX, int screenY)
    {
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);
        AnimatedGameObject bomb =
            new AnimatedGameObject(Path.Combine("Assets", "BombExploding.png"), _renderer, 2, 13, 13, 1,
                worldCoords.X, worldCoords.Y);
        _gameObjects.Add(bomb.Id, bomb);
        ++_bombIds;

         // Increment bombs placed stat
        _player?.IncrementBombsPlaced();
    }

    public void RenderTerrain()
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
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
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
