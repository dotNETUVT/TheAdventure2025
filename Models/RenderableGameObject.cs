using Silk.NET.SDL;

namespace TheAdventure.Models;

public class RenderableGameObject : GameObject
{
    public SpriteSheet SpriteSheet { get; set; }
    public (int X, int Y) Position { get; set; }
    public double Angle { get; set; }
    public Point RotationCenter { get; set; }

    public RenderableGameObject(SpriteSheet spriteSheet, (int X, int Y) position, double angle = 0.0,
        Point rotationCenter = new())
        : base()
    {
        SpriteSheet = spriteSheet;
        Position = position;
        Angle = angle;
        RotationCenter = rotationCenter;
    }

    public virtual void Render(GameRenderer renderer)
    {
        SpriteSheet.Render(renderer, Position, Angle, RotationCenter);
    }
}

public class DialogueNode
{
    public string Line { get; set; } = "";
    public string[]? Choices { get; set; }
    public DialogueNode[]? Next { get; set; }
}

public class Wizard : RenderableGameObject
{
    private DialogueNode? _currentNode;
    private int _choiceIndex = 0;

    public bool _justChose = false;
    public bool PlayerNearby { get; set; }
    public bool HasDialogue => _currentNode != null;

    public bool AwaitingChoice => _currentNode?.Choices != null && _currentNode.Next != null;

    public Wizard(SpriteSheet spriteSheet, (int X, int Y) position) : base(spriteSheet, position)
    {
        SpriteSheet.ActivateAnimation(null);
        InitializeDialogue();
    }

    private void InitializeDialogue()
    {
        var wisdomNode = new DialogueNode { Line = "Then you must study the scrolls." };
        var powerNode = new DialogueNode { Line = "Then take this wand and go forth." };

        var root = new DialogueNode
        {
            Line = "Do you seek wisdom or power?",
            Choices = new[] { "Wisdom", "Power" },
            Next = new[] { wisdomNode, powerNode }
        };

        var greetingNode = new DialogueNode
        {
            Line = "Hello adventurer!",
            Next = new[] { root } 
        };

        _currentNode = greetingNode;
    }

    public string? GetCurrentLine() => PlayerNearby ? _currentNode?.Line : null;
    public string[]? GetChoices() => PlayerNearby ? _currentNode?.Choices : null;

    public void Advance()
    {
        if (!PlayerNearby || _currentNode == null) return;

        if (_justChose)
        {
            _justChose = false;
            return;
        }

        if (_currentNode.Next != null && _currentNode.Next.Length > 0)
        {
            _currentNode = _currentNode.Next[0]; 
        }
        else
        {
            _currentNode = null; 
        }
    }


    public void Choose(int index)
    {
        if (!AwaitingChoice || _currentNode?.Next == null) return;
        if (index >= 0 && index < _currentNode.Next.Length)
        {
            _currentNode = _currentNode.Next[index];
            _justChose = true;
        }
    }

    public void NavigateChoice(int direction)
    {
        if (!AwaitingChoice || _currentNode?.Choices == null) return;
        _choiceIndex = (_choiceIndex + direction + _currentNode.Choices.Length) % _currentNode.Choices.Length;
    }

    public int GetSelectedChoiceIndex() => _choiceIndex;
}
