using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class EnemyObject : RenderableGameObject
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;
    private const int Speed = 64;

    public EnemyObject(GameRenderer renderer, int startX, int startY)
        : base(
            spriteSheet: new SpriteSheet(renderer, Path.Combine("Assets", "player.png"), 1, 1, 48, 48, (24, 24)), // Pass spriteSheet
            position: (startX, startY),
            angle: 0
        )
    {
        X = startX;
        Y = startY;
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load enemy texture");
        }
        UpdateTarget();
    }

    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer);
    }

    public void Update(PlayerObject player, int time)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        double directionX = player.X + 48 - X;
        double directionY = player.Y - 20 - Y;

        double distance = Math.Sqrt(directionX * directionX + directionY * directionY);
        if (distance > 0)
        {
            directionX /= distance;
            directionY /= distance;
        }

        X += (int)Math.Round(directionX * pixelsToMove);
        Y += (int)Math.Round(directionY * pixelsToMove);

        Position = (X, Y);

        UpdateTarget();
    }

    private void UpdateTarget()
    {
        _target = new(X + 24, Y - 42, 48, 48);
    }
}
