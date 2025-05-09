using Silk.NET.Maths;

namespace TheAdventure.Models;

public class CollectibleObject : RenderableGameObject
{
    private double _bobTimer = 0;
    private double _initialY;
    private const double BOB_AMPLITUDE = 4.0;
    private const double BOB_FREQUENCY = 2.0;
    public int PowerValue { get; }
    
    public CollectibleObject(SpriteSheet spriteSheet, int x, int y, int powerValue = 1) 
        : base(spriteSheet, (x, y), 0, default)
    {
        _initialY = y;
        PowerValue = powerValue;
        spriteSheet.ActivateAnimation("Default");
    }
    
    public void Update(double deltaTime)
    {
        _bobTimer += deltaTime / 1000.0;
        
        int newY = (int)(_initialY + Math.Sin(_bobTimer * BOB_FREQUENCY * Math.PI) * BOB_AMPLITUDE);
        Position = (Position.X, newY);
    }
    
    public Rectangle<double> GetBounds()
    {
        return new Rectangle<double>(
            Position.X - 16, 
            Position.Y - 16, 
            32, 
            32
        );
    }
}