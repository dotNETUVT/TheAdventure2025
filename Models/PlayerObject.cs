using Silk.NET.Maths;
using Silk.NET.SDL; // Required for RendererFlip and Point
using System.Collections.Generic; // Required for List<T>
using System; // Required for Enum.GetName

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject // Existing inheritance [cite: uploaded:TheAdventure2025/Models/RenderableGameObject.cs]
{
    // --- New properties for weapon handling ---
    public Weapon? EquippedWeapon { get; private set; }
    private List<Weapon> _knownWeapons = new List<Weapon>();
    // --- End new properties ---

    private const int _speed = 128; // pixels per second [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]

    // --- Original Enums ---
    public enum PlayerStateDirection
    {
        None = 0,
        Down,
        Up,
        Left,
        Right,
    } // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]

    public enum PlayerState
    {
        None = 0,
        Idle,
        Move,
        Attack,
        GameOver
    } // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    // --- End Original Enums ---

    // --- Original State Property ---
    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; } // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    // --- End Original State Property ---

    // --- Original Constructor ---
    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : base(spriteSheet, (x, y)) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    {
        SetState(PlayerState.Idle, PlayerStateDirection.Down); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    }
    // --- End Original Constructor ---

    // --- New methods for weapon handling ---
    public void AddKnownWeapon(Weapon weapon)
    {
        if (_knownWeapons.Count < 4 && !_knownWeapons.Contains(weapon)) // Example: limit to 4, can be adjusted
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
    // --- End new weapon methods ---

    // --- Modified Render method ---
    public override void Render(GameRenderer renderer)
    {
        base.Render(renderer); // Renders the player's own SpriteSheet

        if (EquippedWeapon != null &&
            EquippedWeapon.EquippedTextureId != -1 &&
            State.State == PlayerState.Attack &&
            SpriteSheet != null && !SpriteSheet.AnimationFinished) // Check if player attack animation is playing [cite: uploaded:TheAdventure2025/Models/SpriteSheet.cs]
        {
            int weaponTexId = EquippedWeapon.EquippedTextureId;
            int weaponSpriteW = EquippedWeapon.EquippedSpriteWidth;
            int weaponSpriteH = EquippedWeapon.EquippedSpriteHeight;

            if (weaponSpriteW <= 0 || weaponSpriteH <= 0) return;

            Vector2D<int> weaponOffset = new Vector2D<int>(0, 0);
            double weaponAngle = 0.0;
            RendererFlip weaponFlip = RendererFlip.None;
            Point weaponRotationCenter = new Point(0, weaponSpriteH / 2); // Default: handle on the left, middle height

            // These offsets and angles are EXAMPLES and will need significant fine-tuning
            // based on your actual player and weapon sprites to look correct.
            // SpriteSheet.FrameCenter.OffsetX/Y is from player's sprite sheet definition [cite: uploaded:TheAdventure2025/Models/SpriteSheet.cs, uploaded:TheAdventure2025/bin/Debug/net9.0/Assets/Player.json]
            switch (State.Direction)
            {
                case PlayerStateDirection.Down:
                    // Example: Position weapon in front and slightly below player's center, angled down
                    weaponOffset = new Vector2D<int>(SpriteSheet.FrameCenter.OffsetX - (weaponSpriteW / 2), SpriteSheet.FrameCenter.OffsetY + 10); 
                    weaponAngle = 70.0;
                    weaponRotationCenter = new Point(weaponSpriteW / 2, 0); // Rotate around top-center of weapon sprite
                    break;
                case PlayerStateDirection.Up:
                    weaponOffset = new Vector2D<int>(SpriteSheet.FrameCenter.OffsetX - (weaponSpriteW / 2), -SpriteSheet.FrameCenter.OffsetY - weaponSpriteH + 10 );
                    weaponAngle = -110.0;
                    weaponRotationCenter = new Point(weaponSpriteW / 2, weaponSpriteH); // Rotate around bottom-center
                    break;
                case PlayerStateDirection.Left:
                    weaponOffset = new Vector2D<int>(-SpriteSheet.FrameCenter.OffsetX - weaponSpriteW + 30, SpriteSheet.FrameCenter.OffsetY - (weaponSpriteH / 2) -10);
                    weaponAngle = 0.0; 
                    weaponFlip = RendererFlip.Horizontal; // Assuming weapon sprite faces right by default
                    weaponRotationCenter = new Point(weaponSpriteW, weaponSpriteH / 2); // Rotate around its right edge (handle)
                    break;
                case PlayerStateDirection.Right:
                     weaponOffset = new Vector2D<int>(SpriteSheet.FrameCenter.OffsetX - 30, SpriteSheet.FrameCenter.OffsetY - (weaponSpriteH / 2) -10);
                    weaponAngle = 0.0;
                    // weaponRotationCenter = new Point(0, weaponSpriteH / 2); // Default, handle on left
                    break;
            }

            var weaponRenderX = Position.X + weaponOffset.X;
            var weaponRenderY = Position.Y + weaponOffset.Y;

            Rectangle<int> weaponSrcRect = new Rectangle<int>(0, 0, weaponSpriteW, weaponSpriteH);
            Rectangle<int> weaponDestRect = new Rectangle<int>(weaponRenderX, weaponRenderY, weaponSpriteW, weaponSpriteH);
            
            renderer.RenderTexture(weaponTexId, weaponSrcRect, weaponDestRect, weaponFlip, weaponAngle, weaponRotationCenter);
        }
    }
    // --- End Modified Render method ---

    // --- Original Methods ---
    public void SetState(PlayerState state)
    {
        SetState(state, State.Direction); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    }

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            return; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }

        if (State.State == state && State.Direction == direction) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            return; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }

        if (SpriteSheet == null) return; // Safety check

        if (state == PlayerState.None && direction == PlayerStateDirection.None) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            SpriteSheet.ActivateAnimation(null); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }
        else if (state == PlayerState.GameOver) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            SpriteSheet.ActivateAnimation(Enum.GetName(state)); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }
        else
        {
            var animationName = Enum.GetName(state) + Enum.GetName(direction); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            SpriteSheet.ActivateAnimation(animationName); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }

        State = (state, direction); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    }

    public void GameOver()
    {
        SetState(PlayerState.GameOver, PlayerStateDirection.None); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    }

    public void Attack()
    {
        if (State.State == PlayerState.GameOver) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            return; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }

        var direction = State.Direction; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        SetState(PlayerState.Attack, direction); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (State.State == PlayerState.GameOver) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            return; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }

        var pixelsToMove = _speed * (time / 1000.0); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]

        var x = Position.X + (int)(right * pixelsToMove); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        x -= (int)(left * pixelsToMove); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]

        var y = Position.Y + (int)(down * pixelsToMove); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        y -= (int)(up * pixelsToMove); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]

        var newState = State.State; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        var newDirection = State.Direction; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]

        if (x == Position.X && y == Position.Y) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            if (State.State == PlayerState.Attack) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            {
                if (SpriteSheet != null && SpriteSheet.AnimationFinished) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
                {
                    newState = PlayerState.Idle; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
                }
            }
            else
            {
                newState = PlayerState.Idle; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            }
        }
        else
        {
            newState = PlayerState.Move; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            
            if (y < Position.Y && newDirection != PlayerStateDirection.Up) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            {
                newDirection = PlayerStateDirection.Up; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            }

            if (y > Position.Y && newDirection != PlayerStateDirection.Down) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            {
                newDirection = PlayerStateDirection.Down; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            }

            if (x < Position.X && newDirection != PlayerStateDirection.Left) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            {
                newDirection = PlayerStateDirection.Left; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            }

            if (x > Position.X && newDirection != PlayerStateDirection.Right) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            {
                newDirection = PlayerStateDirection.Right; // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
            }
        }

        if (newState != State.State || newDirection != State.Direction) // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        {
            SetState(newState, newDirection); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
        }

        Position = (x, y); // [cite: uploaded:TheAdventure2025/Models/PlayerObject.cs]
    }
    // --- End Original Methods ---
}