
using Silk.NET.Maths;
namespace TheAdventure.Models.Data;

using TheAdventure.Models;

public class EnemyObject : RenderableGameObject
{
    private readonly PlayerObject _player;
    private const int Speed = 64;
    private string _state = "Chase";
    private bool _isDying = false;
    private DateTimeOffset _dieStartTime;
    private const int DieAnimationDuration = 400; 
    private double _posX;
    private double _posY;

    public EnemyObject(GameRenderer renderer, PlayerObject player, int startX, int startY)
        : base(
            new SpriteSheet(renderer,
                Path.Combine("Assets", "Bat_Sprite_Sheet.png"),
                rowCount: 3, columnCount: 5, frameWidth: 16, frameHeight: 24, frameCenter: (8, 12)),
            (startX, startY))
    {
        _player = player;
        _posX = startX;
        _posY = startY;
        Position = (startX, startY);

        SpriteSheet.Animations["Fly"] = new SpriteSheet.Animation
        {
            StartFrame = (1, 0),
            EndFrame = (1, 5),
            DurationMs = 400,
            Loop = true
        };

        SpriteSheet.Animations["Damage"] = new SpriteSheet.Animation
        {
            StartFrame = (1, 0),
            EndFrame = (1, 3),
            DurationMs = 300,
            Loop = false
        };

        SpriteSheet.Animations["Die"] = new SpriteSheet.Animation
        {
            StartFrame = (2, 0),
            EndFrame = (2, 3),
            DurationMs = 400,
            Loop = false
        };

        SpriteSheet.ActivateAnimation("Fly");
    }

    private static bool RectanglesIntersect(Rectangle<int> a, Rectangle<int> b)
    {
        int ax = a.Origin.X;
        int ay = a.Origin.Y;
        int aw = a.Size.X;
        int ah = a.Size.Y;

        int bx = b.Origin.X;
        int by = b.Origin.Y;
        int bw = b.Size.X;
        int bh = b.Size.Y;

        return ax < bx + bw &&
               ax + aw > bx &&
               ay < by + bh &&
               ay + ah > by;
    }

    public override void Update(int elapsedMs)
    {
        if (_isDying)
        {
            if ((DateTimeOffset.Now - _dieStartTime).TotalMilliseconds >= DieAnimationDuration)
            {
                
            }
            return;
        }
        
        if (_player.IsAttacking())
        {
            var swordHitBox = _player.GetSwordHitBox();
            var enemyHitBox = new Rectangle<int>(
                Position.X - 8,
                Position.Y - 12,
                16,
                24
            );

            if (RectanglesIntersect(swordHitBox, enemyHitBox))
            {
                _isDying = true;
                _dieStartTime = DateTimeOffset.Now;
                SpriteSheet.ActivateAnimation("Die");
                _state = "Dead";
                return;
            }
        }
        
        var playerHitBox = new Rectangle<int>(_player.X - 24, _player.Y - 24, 48, 48);
        var myHitBox = new Rectangle<int>(Position.X - 8, Position.Y - 12, 16, 24);

        if (RectanglesIntersect(playerHitBox, myHitBox))
        {
            _player.TriggerDeath();
            return;
        }
        if (_state == "Chase")
        {
            double dx = _player.X - _posX;
            double dy = _player.Y - _posY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > 1.0)
            {
                double moveAmount = Speed * (elapsedMs / 1000.0);

                _posX += moveAmount * dx / distance;
                _posY += moveAmount * dy / distance;

                Position = ((int)_posX, (int)_posY);
            }
        }
    }
    public void ForceDeath()
    {
        if (_isDying) return;
        _isDying = true;
        _dieStartTime = DateTimeOffset.Now;
        SpriteSheet.ActivateAnimation("Die");
        _state = "Dead";
    }


}

