using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source = new(0, 0, 48, 48);
    private Rectangle<int> _target = new(0, 0, 48, 48);

    private readonly int _textureId;

    private const int Speed = 128;

    public PlayerObject(GameRenderer renderer)
    {
        _textureId = renderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        UpdateTarget();
    }

    public void UpdatePosition(double up, double down, double left, double right, int time, IEnumerable<TemporaryGameObject> bombs)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        int newX = X - (int)(pixelsToMove * left) + (int)(pixelsToMove * right);
        int newY = Y - (int)(pixelsToMove * up) + (int)(pixelsToMove * down);

        var playerRect = new Rectangle<int>(newX + 24, newY - 42, 48, 48);


        bool collidesWithBomb = bombs.Any(bomb =>
    {
        var bombRect = new Rectangle<int>(bomb.Position.X, bomb.Position.Y, 48, 48);

        int bombLeft = bombRect.Origin.X;
        int bombRight = bombRect.Origin.X + bombRect.Size.X;
        int bombTop = bombRect.Origin.Y;
        int bombBottom = bombRect.Origin.Y + bombRect.Size.Y;

        int playerLeft = playerRect.Origin.X;
        int playerRight = playerRect.Origin.X + playerRect.Size.X;
        int playerTop = playerRect.Origin.Y;
        int playerBottom = playerRect.Origin.Y + playerRect.Size.Y;

        return bombLeft < playerRight &&
            bombRight > playerLeft &&
            bombTop < playerBottom &&
            bombBottom > playerTop;
    });



        if (!collidesWithBomb)
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
        _target = new(X + 24, Y - 42, 48, 48);
    }
}
