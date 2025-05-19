using Silk.NET.Maths;
using System;
using System.Threading.Tasks;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    private const int _fenceThickness = 16; // thickness of the boundary fence
    private int _mapWidth = 1000; // default map width
    private int _mapHeight = 1000; // default map height
    private const int _playerWidth = 32; // approximate player sprite width
    private const int _playerHeight = 32; // approximate player sprite height
    
    // Large fixed bottom boundary to prevent player from going below a certain point
    private int _bottomBoundaryHeight = 360; // using a large fixed value for reliability
    
    public enum PlayerStateDirection
    {
        None = 0,
        Down,
        Up,
        Left,
        Right,
    }

    public enum PlayerState
    {
        None = 0,
        Idle,
        Move,
        Attack,
        GameOver
    }

    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y))
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
        
        // Initialize with default map dimensions
        // These should be updated when the actual map is loaded
        SetMapBoundaries(1000, 1000);
    }
    
    // Method to set map boundaries
    public void SetMapBoundaries(int width, int height)
    {
        _mapWidth = width;
        _mapHeight = height;
        
        // Set a fixed, large bottom boundary (500 pixels)
        // This ensures the player stays well above the bottom of the map
        _bottomBoundaryHeight = 360;
    }

    // Method to specifically set the bottom boundary height
    public void SetBottomBoundaryHeight(int height)
    {
        _bottomBoundaryHeight = height;
    }
    
    public void SetState(PlayerState state)
    {
        SetState(state, State.Direction);
    }

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        if (State.State == state && State.Direction == direction)
        {
            return;
        }

        if (state == PlayerState.None && direction == PlayerStateDirection.None)
        {
            SpriteSheet.ActivateAnimation(null);
        }

        else if (state == PlayerState.GameOver)
        {
            SpriteSheet.ActivateAnimation(Enum.GetName(state));
        }
        else
        {
            var animationName = Enum.GetName(state) + Enum.GetName(direction);
            SpriteSheet.ActivateAnimation(animationName);
        }

        State = (state, direction);
    }

    public void GameOver()
    {
        SetState(PlayerState.GameOver, PlayerStateDirection.None);
    }

    public void Attack()
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var direction = State.Direction;
        SetState(PlayerState.Attack, direction);
    }
    
    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        // Apply boundaries - prevent player from going beyond map edges
        
        // Left boundary
        if (x < _fenceThickness)
        {
            x = _fenceThickness;
        }
        
        // Right boundary - account for player width
        if (x > _mapWidth - _playerWidth - _fenceThickness)
        {
            x = _mapWidth - _playerWidth - _fenceThickness;
        }
        
        // Top boundary
        if (y < _fenceThickness)
        {
            y = _fenceThickness;
        }
        
        // Special bottom boundary with a fixed, very large restricted area
        // Hard-code the position to ensure the boundary works correctly
        int maxYPosition = _mapHeight - _bottomBoundaryHeight;
        if (y > maxYPosition)
        {
            y = maxYPosition;
        }
        
        var newState = State.State;
        var newDirection = State.Direction;

        if (x == Position.X && y == Position.Y)
        {
            if (State.State == PlayerState.Attack)
            {
                if (SpriteSheet.AnimationFinished)
                {
                    newState = PlayerState.Idle;
                }
            }
            else
            {
                newState = PlayerState.Idle;
            }
        }
        else
        {
            newState = PlayerState.Move;
            
            if (y < Position.Y && newDirection != PlayerStateDirection.Up)
            {
                newDirection = PlayerStateDirection.Up;
            }

            if (y > Position.Y && newDirection != PlayerStateDirection.Down)
            {
                newDirection = PlayerStateDirection.Down;
            }

            if (x < Position.X && newDirection != PlayerStateDirection.Left)
            {
                newDirection = PlayerStateDirection.Left;
            }

            if (x > Position.X && newDirection != PlayerStateDirection.Right)
            {
                newDirection = PlayerStateDirection.Right;
            }
        }

        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }
}
