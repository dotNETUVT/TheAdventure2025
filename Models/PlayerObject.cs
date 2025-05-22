using Silk.NET.Maths;
using TheAdventure.Extensions;
using TheAdventure.Models;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;


    public int MaxHealth { get; private set; } = 100;
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;
    public int TextureId => _textureId;
    public Rectangle<int> SourceRect => _source;


    public int Width { get; } = 48;
    public int Height { get; } = 48;
    public int CenterOffsetX { get; } = 24;
    public int CenterOffsetY { get; } = 42;


    private const int Speed = 128;

    public PlayerObject(GameRenderer renderer)
    {
        CurrentHealth = MaxHealth;
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }
        UpdateTarget();
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        CurrentHealth -= amount;
        if (CurrentHealth < 0)
        {
            CurrentHealth = 0;
        }
    }

    public void UpdatePosition(double up, double down, double left, double right, int time, IEnumerable<TemporaryGameObject> bombs)
    {
        if (!IsAlive) return;

        var pixelsToMove = Speed * (time / 1000.0);

        int newX = X - (int)(pixelsToMove * left) + (int)(pixelsToMove * right);
        int newY = Y - (int)(pixelsToMove * up) + (int)(pixelsToMove * down);

        var playerCollisionRect = new Rectangle<int>(newX - CenterOffsetX, newY - CenterOffsetY, Width, Height);

        bool collidesWithBombObstacle = bombs.Any(bomb =>
        {
            int bombObstacleWidth = 32;
            int bombObstacleHeight = 32;
            int bombEffectiveX = bomb.Position.X - bombObstacleWidth / 2;
            int bombEffectiveY = bomb.Position.Y - bombObstacleHeight / 2;
            var bombRect = new Rectangle<int>(bombEffectiveX, bombEffectiveY, bombObstacleWidth, bombObstacleHeight);

            return playerCollisionRect.IntersectsWith(bombRect);
        });

        if (!collidesWithBombObstacle)
        {
            X = newX;
            Y = newY;
            UpdateTarget();
        }
    }

    public void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(_textureId, _source, _target);
    }

    private void UpdateTarget()
    {
        _target = new Rectangle<int>(X - CenterOffsetX, Y - CenterOffsetY, Width, Height);
    }

    public Rectangle<int> GetBoundingBox()
    {
        return _target;
    }
}