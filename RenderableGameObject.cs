using Silk.NET.Maths;

namespace TheAdventure;

public class RenderableGameObject : GameObject
{
    public int TextureId { get; }
    public Rectangle<int> TextureSource { get; set; }
    public Rectangle<int> TextureDestination { get; set; }
    public GameRenderer.TextureInfo TextureInformation { get; }

    public RenderableGameObject(int textureId = -1, Rectangle<int> textureSource = new(),
        Rectangle<int> textureDestination = new(),
        GameRenderer.TextureInfo textureInfo = default)
    {
        TextureId = textureId;
        TextureSource = textureSource;
        TextureDestination = textureDestination;
        TextureInformation = textureInfo;
    }
}