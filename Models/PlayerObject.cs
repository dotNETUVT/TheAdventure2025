using Silk.NET.Maths;
using Silk.NET.SDL; 
using System.Collections.Generic; 
using System; 

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject 
{
 
    public Weapon? EquippedWeapon { get; private set; }
    private List<Weapon> _knownWeapons = new List<Weapon>();


    private const int _speed = 128; 


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
    }
    

   
    public void AddKnownWeapon(Weapon weapon)
    {
        if (_knownWeapons.Count < 4 && !_knownWeapons.Contains(weapon)) 
        {
            _knownWeapons.Add(weapon);
            if (EquippedWeapon == null && _knownWeapons.Count > 0)
            {
                EquipWeaponByIndex(0);
            }
        }
    }

    public void EquipWeaponByIndex(int weaponIndex)
    {
        if (State.State == PlayerState.GameOver) return;

        if (weaponIndex >= 0 && weaponIndex < _knownWeapons.Count)
        {
            EquippedWeapon = _knownWeapons[weaponIndex];
            Console.WriteLine($"Player equipped: {EquippedWeapon?.Name ?? "Nothing"}");
        }
    }
   

    
    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer); 

        if (EquippedWeapon != null &&
            EquippedWeapon.EquippedTextureId != -1 &&
            State.State == PlayerState.Attack &&
            SpriteSheet != null && !SpriteSheet.AnimationFinished) 
        {
            int weaponTexId = EquippedWeapon.EquippedTextureId;
            int weaponSpriteW = EquippedWeapon.EquippedSpriteWidth;
            int weaponSpriteH = EquippedWeapon.EquippedSpriteHeight;

            if (weaponSpriteW <= 0 || weaponSpriteH <= 0) return;

            Vector2D<int> weaponOffset = new Vector2D<int>(0, 0);
            double weaponAngle = 0.0;
            RendererFlip weaponFlip = RendererFlip.None;
            Point weaponRotationCenter = new Point(0, weaponSpriteH / 2); 

            
            
            switch (State.Direction)
            {
                case PlayerStateDirection.Down:
                    
                    weaponOffset = new Vector2D<int>(SpriteSheet.FrameCenter.OffsetX - (weaponSpriteW / 2), SpriteSheet.FrameCenter.OffsetY + 10); 
                    weaponAngle = 70.0;
                    weaponRotationCenter = new Point(weaponSpriteW / 2, 0); 
                    break;
                case PlayerStateDirection.Up:
                    weaponOffset = new Vector2D<int>(SpriteSheet.FrameCenter.OffsetX - (weaponSpriteW / 2), -SpriteSheet.FrameCenter.OffsetY - weaponSpriteH + 10 );
                    weaponAngle = -110.0;
                    weaponRotationCenter = new Point(weaponSpriteW / 2, weaponSpriteH); 
                    break;
                case PlayerStateDirection.Left:
                    weaponOffset = new Vector2D<int>(-SpriteSheet.FrameCenter.OffsetX - weaponSpriteW + 30, SpriteSheet.FrameCenter.OffsetY - (weaponSpriteH / 2) -10);
                    weaponAngle = 0.0; 
                    weaponFlip = RendererFlip.Horizontal; 
                    weaponRotationCenter = new Point(weaponSpriteW, weaponSpriteH / 2); 
                    break;
                case PlayerStateDirection.Right:
                     weaponOffset = new Vector2D<int>(SpriteSheet.FrameCenter.OffsetX - 30, SpriteSheet.FrameCenter.OffsetY - (weaponSpriteH / 2) -10);
                    weaponAngle = 0.0;
                    
                    break;
            }

            var weaponRenderX = Position.X + weaponOffset.X;
            var weaponRenderY = Position.Y + weaponOffset.Y;

            Rectangle<int> weaponSrcRect = new Rectangle<int>(0, 0, weaponSpriteW, weaponSpriteH);
            Rectangle<int> weaponDestRect = new Rectangle<int>(weaponRenderX, weaponRenderY, weaponSpriteW, weaponSpriteH);
            
            renderer.RenderTexture(weaponTexId, weaponSrcRect, weaponDestRect, weaponFlip, weaponAngle, weaponRotationCenter);
        }
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

        if (SpriteSheet == null) return; // Safety check

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

        var newState = State.State; 
        var newDirection = State.Direction; 

        if (x == Position.X && y == Position.Y) 
        {
            if (State.State == PlayerState.Attack) 
            {
                if (SpriteSheet != null && SpriteSheet.AnimationFinished) 
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