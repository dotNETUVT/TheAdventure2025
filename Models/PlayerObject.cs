using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private string _currentAnimation = "IdleDown";
    private bool _isAttacking = false;
    private bool _isLayingDown = false;
    private bool _isPaused = false;

    private DateTimeOffset _attackStartTime;
    private DateTimeOffset _layDownStartTime;
    private const double _attackDuration = 0.5; // seconds
    private const double _layDownAnimDuration = 0.5; // seconds
    private const double _layDownTotalDuration = 3.0; // seconds (0.5s anim + 2.5s stay)
    private Rectangle<int> _worldBounds;
    private string _lastDirection = "Down";

    // Offsets for border collision
    private const int TOP_OFFSET = 24; // Full player height to avoid disappearing at top
    private const int SIDE_OFFSET = 12; // Half player width to avoid half visibility at sides

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
        _worldBounds = new Rectangle<int>(0, 0, 1000, 1000); // Default bounds
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        // Adjust bounds to account for player size and visibility
        _worldBounds = new Rectangle<int>(
            bounds.Origin.X + SIDE_OFFSET,
            bounds.Origin.Y + TOP_OFFSET,
            bounds.Size.X - (SIDE_OFFSET * 2),
            bounds.Size.Y - TOP_OFFSET
        );
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        SpriteSheet.SetPaused(paused);

        // If we're attacking or laying down, we need to adjust the timers
        if (_isAttacking && paused)
        {
            // When pausing, we'll need to extend the animation duration
            _attackStartTime = DateTimeOffset.Now.AddMilliseconds(
                -((DateTimeOffset.Now - _attackStartTime).TotalMilliseconds));
        }

        if (_isLayingDown && paused)
        {
            // When pausing, we'll need to extend the animation duration
            _layDownStartTime = DateTimeOffset.Now.AddMilliseconds(
                -((DateTimeOffset.Now - _layDownStartTime).TotalMilliseconds));
        }
    }
    public void Attack()
    {
        if (_isAttacking || _isLayingDown || _isPaused) return;


        _isAttacking = true;
        _attackStartTime = DateTimeOffset.Now;

        // Set attack animation based on last direction
        switch (_lastDirection)
        {
            case "Up":
                SpriteSheet.ActivateAnimation("AttackUp");
                break;
            case "Down":
                SpriteSheet.ActivateAnimation("AttackDown");
                break;
            case "Left":
                SpriteSheet.ActivateAnimation("AttackSide");
                SpriteSheet.ActiveAnimation!.Flip = RendererFlip.Horizontal;
                break;
            case "Right":
            default:
                SpriteSheet.ActivateAnimation("AttackSide");
                SpriteSheet.ActiveAnimation!.Flip = RendererFlip.None;
                break;
        }
    }

    public void LayDown()
    {
        if (_isLayingDown || _isAttacking || _isPaused) return;

        _isLayingDown = true;
        _layDownStartTime = DateTimeOffset.Now;
        SpriteSheet.ActivateAnimation("LayDown");
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        // Don't update anything if paused
        if (_isPaused) return;

        // Check if lay down is finished
        if (_isLayingDown)
        {
            double elapsedTime = (DateTimeOffset.Now - _layDownStartTime).TotalSeconds;
            if (elapsedTime >= _layDownTotalDuration)
            {
                _isLayingDown = false;
                // Return to last idle animation after laying down
                SpriteSheet.ActivateAnimation("Idle" + _lastDirection);
            }
            return; // Don't move while laying down
        }

        // Check if attack is finished
        if (_isAttacking)
        {
            if ((DateTimeOffset.Now - _attackStartTime).TotalSeconds >= _attackDuration)
            {
                _isAttacking = false;
                // Return to last idle animation after attack
                SpriteSheet.ActivateAnimation("Idle" + _lastDirection);
            }
            return; // Don't move while attacking
        }

        if (up + down + left + right == 0)
        {
            return;
        }

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        // Keep player within world bounds
        x = Math.Clamp(x, _worldBounds.Origin.X, _worldBounds.Origin.X + _worldBounds.Size.X);
        y = Math.Clamp(y, _worldBounds.Origin.Y, _worldBounds.Origin.Y + _worldBounds.Size.Y);

        var newAnimation = _currentAnimation;

        if (y < Position.Y && (newAnimation != "MoveUp" || _currentAnimation != "MoveUp"))
        {
            newAnimation = "MoveUp";
            _lastDirection = "Up";
        }

        if (y > Position.Y && (newAnimation != "MoveDown" || _currentAnimation != "MoveDown"))
        {
            newAnimation = "MoveDown";
            _lastDirection = "Down";
        }

        if (x < Position.X && (newAnimation != "MoveLeft" || _currentAnimation != "MoveLeft"))
        {
            newAnimation = "MoveLeft";
            _lastDirection = "Left";
        }

        if (x > Position.X && (newAnimation != "MoveRight" || _currentAnimation != "MoveRight"))
        {
            newAnimation = "MoveRight";
            _lastDirection = "Right";
        }

        if (x == Position.X && y == Position.Y && newAnimation != "Idle" + _lastDirection)
        {
            newAnimation = "Idle" + _lastDirection;
        }

        if (newAnimation != _currentAnimation)
        {
            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }

        Position = (x, y);
    }
}