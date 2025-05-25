using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure.UI;

public class Button
{
    public string Text { get; set; }
    public Rectangle<int> Bounds { get; set; }
    public bool IsHovered { get; private set; }
    public Action? OnClick { get; set; }

    public Button(string text, int x, int y, int width, int height)
    {
        Text = text;
        Bounds = new Rectangle<int>(x, y, width, height);
    }

    public void Update(int mouseX, int mouseY, bool isClicked)
    {
        IsHovered = mouseX >= Bounds.Origin.X && 
                    mouseX <= Bounds.Origin.X + Bounds.Size.X && 
                    mouseY >= Bounds.Origin.Y && 
                    mouseY <= Bounds.Origin.Y + Bounds.Size.Y;

        if (IsHovered && isClicked)
        {
            OnClick?.Invoke();
        }
    }

    public void Draw(GameRenderer renderer, FontRenderer fontRenderer)
    {
        // Draw button background
        renderer.SetDrawColor(IsHovered ? (byte)90 : (byte)70, IsHovered ? (byte)90 : (byte)70, IsHovered ? (byte)90 : (byte)70, 255);
        renderer.FillRect(Bounds);
        
        // Draw button border
        renderer.SetDrawColor(IsHovered ? (byte)220 : (byte)180, IsHovered ? (byte)220 : (byte)180, IsHovered ? (byte)220 : (byte)180, 255);
        renderer.DrawRect(Bounds);
        
        // Draw button text
        var centerX = Bounds.Origin.X + Bounds.Size.X / 2;
        var centerY = Bounds.Origin.Y + Bounds.Size.Y / 2;
        
        var (textWidth, textHeight) = fontRenderer.MeasureText(Text);
        fontRenderer.RenderText(
            renderer.GetRawRenderer(), 
            Text, 
            centerX, 
            centerY - textHeight / 2,
            255, 255, 255,
            TextAlign.Center);
    }
} 