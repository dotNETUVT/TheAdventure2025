using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure.GameState;

public class PlayingState : IGameState
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;

    private readonly Dictionary<int, GameObject> _gameObjects;
    private readonly Dictionary<int, Tile> _tileIdMap;
    
    private Level _currentLevel;
    private PlayerObject _player;

    public event Action<StateChangeRequest>? OnStateChange;
    // public IGameState? Parent { get; set; } // TODO: Maybese concrete type to limit possible parent states

    public UpdateCallback? UpdateCallback { get; set; }
    public DrawCallback? DrawCallback { get; set; }

    public PlayingState(
        IGameState? parent, // TODO: Use concrete type to limit possible parent states
        GameRenderer renderer,
        Input input,
        Dictionary<int, GameObject> gameObjects,
        Dictionary<int, Tile> tileIdMap,
        Level currentLevel,
        PlayerObject player)
    {
        _renderer = renderer;
        _input = input;
        _gameObjects = gameObjects;
        _tileIdMap = tileIdMap;
        _currentLevel = currentLevel;
        _player = player;
        
        // No use case yet for UpdateCallback and RenderCallback
    }

    public void Enter()
    {
        Console.WriteLine("Entering PlayingState");
        // _player.Position = (100, 100);
    }

    public void Exit()
    {
        Console.WriteLine("Exiting PlayingState");
    }

    public void Update(double deltaTime)
    {
        if (_player == null)
        {
            return;
        }

        if (_input.IsEscapePressed())
        {
            OnStateChange?.Invoke(new StateChangeRequest(
                StateChangeRequest.ChangeTypeEnum.Push,
                GameStateType.Paused));
            return;
        }

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, deltaTime);
        if (isAttacking)
        {
            _player.Attack();
        }

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
    }

    public void Render()
    {
        Draw();
        _renderer.PresentFrame();
    }

    public void Draw()
    {
        DrawCallback?.Invoke();

        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                toRemove.Add(tempGameObject.Id);
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id, out var gameObject);

            if (_player == null)
            {
                continue;
            }

            var tempGameObject = (TemporaryGameObject)gameObject!;
            var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
            var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
            if (deltaX < 32 && deltaY < 32)
            {
                _player.GameOver();
            }
        }

        _player?.Render(_renderer);
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

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
}