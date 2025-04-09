using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.Models;

public class RenderableGameObject : GameObject
{
    public int TextureId { get; }
    public int TextureRotation { get; }
    public Point TextureRotationCenter { get; }
    public Rectangle<int> TextureSource { get; set; }
    public Rectangle<int> TextureDestination { get; set; }
    public TextureData TextureInformation { get; }

    public RenderableGameObject(string fileName, GameRenderer renderer)
    {
        TextureId = renderer.LoadTexture(fileName, out var textureData);
        TextureInformation = textureData;
        TextureSource = new Rectangle<int>(0, 0, textureData.Width, textureData.Height);
        TextureDestination = new Rectangle<int>(0, 0, textureData.Width, textureData.Height);
    }

    public virtual void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(TextureId, TextureSource, TextureDestination);
    }

    public virtual bool Update(double secsSinceLastFrame)
    {
        return true;
    }
}