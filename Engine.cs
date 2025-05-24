using System.Collections;
using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.GameState;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();
    private readonly GameStateManager _gameStateManager = new();
    
    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

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

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));

        IGameState startState = new MainMenuState(null, _renderer, _input);
        startState.OnStateChange += HandleStateChangeRequest;
        _gameStateManager.PushState(startState);
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        _gameStateManager.Update(msSinceLastFrame);

        // Bandaid fix
        if (_gameStateManager.GameStateType == GameStateType.Playing)
        {
            _scriptEngine.ExecuteAll(this);
        }
    }

    public void RenderFrame()
    {
        _gameStateManager.Render();
    }

    public (int X, int Y) GetPlayerPosition()
    {
        return _player!.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }

    private IGameState CreateState(GameStateType stateType, IGameState? parent = null)
    {
        return stateType switch
        {
            GameStateType.Playing => new PlayingState(
                                        null,
                                        _renderer,
                                        _input,
                                        _gameObjects,
                                        _tileIdMap,
                                        _currentLevel,
                                        _player!),
            GameStateType.Paused => new PausedState(parent, _renderer, _input),
            GameStateType.MainMenu => new MainMenuState(parent, _renderer, _input),
            _ => throw new Exception($"Unknown game state type: {stateType}")
        };
    }

    private void HandleStateChangeRequest(StateChangeRequest info)
    {
        switch (info.ChangeType)
        {
            case StateChangeRequest.ChangeTypeEnum.Push:
            {
                IGameState gameState = CreateState(info.NewState!.Value, _gameStateManager.TopGameState);
                gameState.OnStateChange += HandleStateChangeRequest;
                _gameStateManager.PushState(gameState);
                _gameStateManager.GameStateType = info.NewState!.Value;
                break;
            }
            case StateChangeRequest.ChangeTypeEnum.Pop:
            {
                _gameStateManager.PopState();
                _gameStateManager.GameStateType = info.NewState!.Value;
                break;
            }
            case StateChangeRequest.ChangeTypeEnum.Change:
            {
                _gameStateManager.OnlyPopTopState();
                IGameState gameState = CreateState(info.NewState!.Value, _gameStateManager.TopGameState);
                gameState.OnStateChange += HandleStateChangeRequest;
                _gameStateManager.PushState(gameState);
                _gameStateManager.GameStateType = info.NewState.Value;
                break;
            }
            case StateChangeRequest.ChangeTypeEnum.PopAll:
            {
                _gameStateManager.PopAllStates();
                IGameState gameState = CreateState(info.NewState!.Value);
                gameState.OnStateChange += HandleStateChangeRequest;
                _gameStateManager.PushState(gameState);
                _gameStateManager.GameStateType = info.NewState!.Value;
                break;
            }
        }
    }
}