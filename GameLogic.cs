using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class GameLogic(GameRenderer renderer)
{
    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private int _bombIds = 0;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    // Timer to control automatic bomb spawning.
    private DateTimeOffset _lastBombSpawn = DateTimeOffset.Now;

    private readonly Random _random = new Random();

    public void InitializeGame()
    {
        _player = new PlayerObject(renderer);

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
                tile.TextureId = renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
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

        const int worldWidth = 800;
        const int worldHeight = 400;

        renderer.SetWorldBounds(new Rectangle<int>(0, 0, worldWidth, worldHeight));

        _currentLevel = level;
    }

    public void ProcessFrame()
    {
        const int timeDistanceBetweenBombs = 2000;
        var currentTime = DateTimeOffset.Now;
        if ((currentTime - _lastBombSpawn).TotalMilliseconds < timeDistanceBetweenBombs)
            return;
        
        AddBomb();
        
        for (var i = 0; i < 10; i++)
        {
            if (_bombIds > 10 * i)
            {
                AddBomb(i * 2);
            }
        }

        _lastBombSpawn = currentTime;
    }

    public void RenderFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        renderer.SetDrawColor(0, 0, 0, 255);
        renderer.ClearScreen();

        renderer.CameraLookAt(_player!.X, _player!.Y);

        RenderTerrain();
        RenderAllObjects(msSinceLastFrame);

        renderer.PresentFrame();
    }

    private void RenderAllObjects(double msSinceLastFrame)
    {
        List<int> itemsToRemove = [];
        foreach (var gameObject in GetRenderables())
        {
            if (gameObject.Update(msSinceLastFrame))
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

    public void UpdatePlayerPosition(double up, double down, double left, double right, int timeSinceLastUpdateInMs,
        bool runKey)
    {
        _player?.UpdatePosition(up, down, left, right, timeSinceLastUpdateInMs, runKey);
    }

    // Automatically spawns a bomb at a random position within an X-tile radius around the player.
    // If there is no X received as param the default is 2
    private void AddBomb(int maxTilesRadius = 2)
    {
        if (_player == null)
        {
            return;
        }

        const int tileSize = 24;
        var angle = _random.NextDouble() * 2 * Math.PI;
        var distanceInTiles = maxTilesRadius * Math.Sqrt(_random.NextDouble());
        var offsetX = (int)(distanceInTiles * Math.Cos(angle) * tileSize);
        var offsetY = (int)(distanceInTiles * Math.Sin(angle) * tileSize);

        var bombX = _player.X + offsetX;
        var bombY = _player.Y + offsetY;

        var bomb = new AnimatedGameObject(
            Path.Combine("Assets", "BombExploding.png"),
            renderer,
            1, 13, 13, 1,
            bombX, bombY
        );
        _gameObjects.Add(bomb.Id, bomb);
        ++_bombIds;
    }

    private void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (var i = 0; i < _currentLevel.Width; ++i)
            {
                for (var j = 0; j < _currentLevel.Height; ++j)
                {
                    var dataIndex = j * currentLayer.Width + i;
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

                    const int tileWidth = 24;
                    const int tileHeight = 24;

                    var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }

    private IEnumerable<RenderableGameObject> GetRenderables()
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