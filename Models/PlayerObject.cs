using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp.PixelFormats;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private readonly SpriteSheet _spriteSheet;
    private string _currentAnimation = "IdleDown";

    private const int Speed = 128; // pixels per second

    private enum Facing { Down, Up, Right }
    private Facing _lastFacing = Facing.Down;
    private bool _facingLeft = false;

    public int BombCount { get; private set; } = 5;

    public PlayerObject(GameRenderer renderer, bool isPlayerOne = false)
    {
        Func<Rgba32, Rgba32>? tint = null;

        if (isPlayerOne)
        {
            // Simple bluish tint: reduce red, boost blue
            tint = color => new Rgba32(
                (byte)(color.R * 0.5),
                (byte)(color.G * 0.6),
                (byte)Math.Min(255, color.B * 1.2),
                color.A
            );
        }
        else
        {
            // Orange ish for Player 2
            tint = color => new Rgba32(
                (byte)Math.Min(255, color.R * 1.4),
                (byte)(color.G * 1.2),
                (byte)(color.B * 0.5),
                color.A
            );
        }

        // load the full player sprite sheet
        _spriteSheet = new SpriteSheet(
            renderer,
            Path.Combine("Assets", "player.png"),
            rowCount: 6,
            columnCount: 6,
            frameWidth: 48,
            frameHeight: 48,
            frameCenter: (24, 42),
            pixelProcessor: tint
        );

        // define animations
        // Idle
        _spriteSheet.Animations["IdleDown"] = new SpriteSheet.Animation { StartFrame = (0, 0), EndFrame = (0, 0), DurationMs = 1000, Loop = true };
        _spriteSheet.Animations["IdleRight"] = new SpriteSheet.Animation { StartFrame = (1, 0), EndFrame = (1, 0), DurationMs = 1000, Loop = true };
        _spriteSheet.Animations["IdleUp"] = new SpriteSheet.Animation { StartFrame = (2, 0), EndFrame = (2, 0), DurationMs = 1000, Loop = true };

        // Walk
        _spriteSheet.Animations["WalkDown"] = new SpriteSheet.Animation { StartFrame = (3, 0), EndFrame = (3, 5), DurationMs = 500, Loop = true };
        _spriteSheet.Animations["WalkRight"] = new SpriteSheet.Animation { StartFrame = (4, 0), EndFrame = (4, 5), DurationMs = 500, Loop = true };
        _spriteSheet.Animations["WalkUp"] = new SpriteSheet.Animation { StartFrame = (5, 0), EndFrame = (5, 5), DurationMs = 500, Loop = true };

        // start idle down
        ActivateAnimation("IdleDown");
    }

    public void UpdatePosition(double up, double down, double left, double right, int deltaMs)
    {
        // 1) Move
        var pixels = Speed * (deltaMs / 1000.0);
        X += (int)(pixels * (right - left));
        Y += (int)(pixels * (down - up));

        // 2) Compute direction
        double vx = right - left;
        double vy = down - up;

        string desiredAnim;
        if (vx == 0 && vy == 0)
        {
            // Idle based on lastFacing:
            desiredAnim = _lastFacing switch
            {
                Facing.Down => "IdleDown",
                Facing.Up => "IdleUp",
                _ => "IdleRight"
            };
        }
        else
        {
            // Walking
            if (Math.Abs(vx) > Math.Abs(vy))
            {
                desiredAnim = "WalkRight";
                // record left vs right
                _facingLeft = vx < 0;
                _lastFacing = Facing.Right;
            }
            else
            {
                if (vy > 0)
                {
                    desiredAnim = "WalkDown";
                    _lastFacing = Facing.Down;
                }
                else
                {
                    desiredAnim = "WalkUp";
                    _lastFacing = Facing.Up;
                }
                // vertical movement doesn’t affect _facingLeft
            }
        }

        // 3) Apply flip from the flag
        _spriteSheet.CurrentFlip = _facingLeft
            ? RendererFlip.Horizontal
            : RendererFlip.None;

        // 4) Swap animation
        ActivateAnimation(desiredAnim);
    }

    public void Render(GameRenderer renderer)
    {
        _spriteSheet.Render(renderer, (X, Y), 0.0, default);
    }

    private void ActivateAnimation(string name)
    {
        if (_currentAnimation == name) return;
        _currentAnimation = name;
        _spriteSheet.ActivateAnimation(name);
    }

    public void AddBomb(int amount = 1) => BombCount += amount;
    public bool TryUseBomb()
    {
        if (BombCount > 0) { BombCount--; return true; }
        return false;
    }

    public void ClampToWorld(Rectangle<int> bounds)
    {
        int left = bounds.Origin.X + 10;
        int right = bounds.Origin.X + bounds.Size.X - 10;
        int top = bounds.Origin.Y + 25;
        int bottom = bounds.Origin.Y + bounds.Size.Y - 25;

        X = Math.Clamp(X, left, right);
        Y = Math.Clamp(Y, top, bottom);
    }
}
