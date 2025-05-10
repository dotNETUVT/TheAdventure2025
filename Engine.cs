using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;

namespace TheAdventure
{
    public class Engine
    {
        private readonly GameRenderer _renderer;
        private readonly Input _input;
        private readonly ScriptEngine _scriptEngine = new();

        private readonly Dictionary<int, GameObject> _gameObjects = new();
        private readonly Dictionary<string, TileSet> _loadedTileSets = new();
        private readonly Dictionary<int, Tile> _tileIdMap = new();

        private Level _currentLevel = new();
        private PlayerObject _player = null!;

        private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

        public int Score { get; private set; } = 0;

        private int _worldWidth;
        private int _worldHeight;
        private readonly Random _rng = new Random();

        private const int InnerRadius = 10;
        private const double BombIntervalMs = 3000;
        private double _bombTimer = 0;
        private double _gemRespawnTimer = 0;
        private const double GemRespawnDelayMs = 2000;

        private SpriteSheet _gemSheet = null!;

        public Engine(GameRenderer renderer, Input input)
        {
            _renderer = renderer;
            _input = input;
            _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
        }

        public void SetupWorld()
        {
            _player = new PlayerObject(
                SpriteSheet.Load(_renderer, "Player.json", "Assets"),
                x: 100, y: 100
            );

            var levelJson = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
            _currentLevel = JsonSerializer.Deserialize<Level>(levelJson)
                            ?? throw new Exception("Failed to load terrain.tmj");

            foreach (var ts in from tsRef in _currentLevel.TileSets
                     let tsJson = File.ReadAllText(Path.Combine("Assets", tsRef.Source))
                     select JsonSerializer.Deserialize<TileSet>(tsJson)
                            ?? throw new Exception($"Failed to load tileset {tsRef.Source}"))
            {
                foreach (var tile in ts.Tiles)
                {
                    var texId = _renderer.LoadTexture(
                        Path.Combine("Assets", tile.Image),
                        out _);
                    tile.TextureId = texId;
                    _tileIdMap[tile.Id!.Value] = tile;
                }

                _loadedTileSets[ts.Name] = ts;
            }

            _worldWidth = _currentLevel.Width!.Value * _currentLevel.TileWidth!.Value;
            _worldHeight = _currentLevel.Height!.Value * _currentLevel.TileHeight!.Value;
            _renderer.SetWorldBounds(new Rectangle<int>(0, 0, _worldWidth, _worldHeight));

            _gemSheet = SpriteSheet.Load(_renderer, "Gems.json", "Assets");
            SpawnGem();

            _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
        }

        public void ProcessFrame()
        {
            var now = DateTimeOffset.Now;
            var delta = (now - _lastUpdate).TotalMilliseconds;
            _lastUpdate = now;

            {
                double up = _input.IsUpPressed() ? 1 : 0;
                double down = _input.IsDownPressed() ? 1 : 0;
                double left = _input.IsLeftPressed() ? 1 : 0;
                double right = _input.IsRightPressed() ? 1 : 0;
                var attack = _input.IsKeyAPressed() && (up + down + left + right <= 1);
                var dropB = _input.IsKeyBPressed();

                _player.UpdatePosition(up, down, left, right, 48, 48, delta);
                if (attack) _player.Attack();
                if (dropB) AddBomb(_player.Position.X, _player.Position.Y, false);
            }

            var collected = new List<int>();
            foreach (var gem in _gameObjects.Values.OfType<GemObject>())
            {
                var dx = Math.Abs(gem.Position.X - _player.Position.X);
                var dy = Math.Abs(gem.Position.Y - _player.Position.Y);
                if (!(dx < gem.SpriteSheet.FrameWidth * 1.5) ||
                    !(dy < gem.SpriteSheet.FrameHeight * 1.5)) continue;
                Score += gem.Value;
                collected.Add(gem.Id);
            }

            if (collected.Count != 0)
            {
                foreach (var id in collected)
                    _gameObjects.Remove(id);
                _gemRespawnTimer = 0;
            }

            if (!_gameObjects.Values.OfType<GemObject>().Any())
            {
                _gemRespawnTimer += delta;
                if (_gemRespawnTimer >= GemRespawnDelayMs)
                {
                    SpawnGem();
                    _gemRespawnTimer = 0;
                }
            }

            _bombTimer += delta;
            if (_bombTimer >= BombIntervalMs)
            {
                _bombTimer -= BombIntervalMs;

                var p = _player.Position;
                var angle = _rng.NextDouble() * Math.PI * 2;
                var dist = _rng.NextDouble() * InnerRadius;
                var bx = p.X + (int)(Math.Cos(angle) * dist);
                var by = p.Y + (int)(Math.Sin(angle) * dist);

                bx = Math.Clamp(bx, 0, _worldWidth);
                by = Math.Clamp(by, 0, _worldHeight);

                AddBomb(bx, by, false);
            }

            _scriptEngine.ExecuteAll(this);
        }

        public void RenderFrame()
        {
            _renderer.SetDrawColor(0, 0, 0, 255);
            _renderer.ClearScreen();

            var p = _player.Position;
            // _renderer.CameraLookAt(p.X, p.Y);

            RenderTerrain();
            RenderAllObjects();

            _renderer.RenderScore(Score);

            _renderer.PresentFrame();
        }

        private void RenderTerrain()
        {
            foreach (var layer in _currentLevel.Layers)
            {
                for (var i = 0; i < _currentLevel.Width; i++)
                for (var j = 0; j < _currentLevel.Height; j++)
                {
                    var idx = j * layer.Width + i;
                    if (idx < 0 || idx >= layer.Data.Count) continue;
                    var gid = layer.Data[idx ?? 0] - 1;
                    if (gid < 0) continue;

                    var tile = _tileIdMap[gid ?? 0];
                    var tw = tile.ImageWidth ?? 0;
                    var th = tile.ImageHeight ?? 0;
                    var src = new Rectangle<int>(0, 0, tw, th);
                    var dst = new Rectangle<int>(i * tw, j * th, tw, th);
                    _renderer.RenderTexture(tile.TextureId, src, dst);
                }
            }
        }

        private void RenderAllObjects()
        {
            var expired = new List<int>();
            foreach (var obj in _gameObjects.Values.OfType<RenderableGameObject>())
            {
                obj.Render(_renderer);
                if (obj is TemporaryGameObject { IsExpired: true } t)
                    expired.Add(t.Id);
            }

            foreach (var id in expired)
            {
                if (!_gameObjects.Remove(id, out var go) || go is not TemporaryGameObject temp) continue;
                var dx = Math.Abs(_player.Position.X - temp.Position.X);
                var dy = Math.Abs(_player.Position.Y - temp.Position.Y);
                if (dx < 32 && dy < 32)
                    _player.GameOver();
            }

            _player.Render(_renderer);
        }

        private void SpawnGem()
        {
            int gx, gy;
            do
            {
                gx = _rng.Next(0, _worldWidth);
                gy = _rng.Next(0, _worldHeight);
            } while (Distance(gx, gy, _player.Position.X, _player.Position.Y) <= InnerRadius);

            var gem = new GemObject(_gemSheet, (gx, gy), value: 1);
            _gameObjects.Add(gem.Id, gem);
        }

        private static double Distance(int x1, int y1, int x2, int y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void AddBomb(int x, int y, bool translateCoords = true)
        {
            var world = translateCoords
                ? _renderer.ToWorldCoordinates(new Vector2D<int>(x, y))
                : new Vector2D<int>(x, y);

            var sheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
            sheet.ActivateAnimation("Explode");

            var bomb = new TemporaryGameObject(sheet, 2.1, (world.X, world.Y));
            _gameObjects.Add(bomb.Id, bomb);
        }
    }
}