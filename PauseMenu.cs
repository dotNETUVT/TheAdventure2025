using Silk.NET.Maths;

namespace TheAdventure;

public class PauseMenu
{
    private readonly GameRenderer _renderer;
    private int _selectedOption = 0; // 0 = Resume, 1 = Quit

    private bool _upPressed = false;
    private bool _downPressed = false;
    private bool _enterPressed = false;
    private bool _shouldResume = false;

    private readonly int _menuOptionWidth = 200;
    private readonly int _menuOptionHeight = 50;
    private readonly int _menuPadding = 20;

    public PauseMenu(GameRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Render()
    {
        // Get window size from renderer
        var windowSize = _renderer.GetWindowSize();

        // Draw a semi-transparent background
        _renderer.SetDrawColor(0, 0, 0, 150);
        var fullScreenRect = new Rectangle<int>(0, 0, windowSize.Width, windowSize.Height);
        _renderer.FillRect(fullScreenRect);

        // Position the menu in the center of the screen
        int menuTotalHeight = 2 * _menuOptionHeight + _menuPadding;
        int menuY = (windowSize.Height - menuTotalHeight) / 2;

        // Draw "PAUSE" title
        _renderer.SetDrawColor(100, 100, 255, 255); // Blue title
        var titleRect = new Rectangle<int>((windowSize.Width - _menuOptionWidth) / 2,
            menuY - _menuOptionHeight - _menuPadding, _menuOptionWidth, _menuOptionHeight);
        _renderer.FillRect(titleRect);

        // Title border
        _renderer.SetDrawColor(255, 255, 255, 255);
        _renderer.DrawRect(titleRect);

        // Create visual "PAUSE" text
        DrawTextPause(titleRect);

        // Resume option
        int resumeY = menuY;
        var resumeRect = new Rectangle<int>((windowSize.Width - _menuOptionWidth) / 2, resumeY, _menuOptionWidth, _menuOptionHeight);

        // Highlight selected option
        if (_selectedOption == 0)
        {
            _renderer.SetDrawColor(0, 220, 0, 255); // Green highlight for Resume
        }
        else
        {
            _renderer.SetDrawColor(100, 100, 100, 255); // Gray for unselected
        }
        _renderer.FillRect(resumeRect);

        // Draw "Resume" text border
        _renderer.SetDrawColor(255, 255, 255, 255);
        _renderer.DrawRect(resumeRect);

        // Draw "RESUME" text
        DrawTextResume(resumeRect);

        // Quit option
        int quitY = resumeY + _menuOptionHeight + _menuPadding;
        var quitRect = new Rectangle<int>((windowSize.Width - _menuOptionWidth) / 2, quitY, _menuOptionWidth, _menuOptionHeight);

        if (_selectedOption == 1)
        {
            _renderer.SetDrawColor(220, 0, 0, 255); // Red highlight for Quit
        }
        else
        {
            _renderer.SetDrawColor(100, 100, 100, 255); // Gray for unselected
        }
        _renderer.FillRect(quitRect);

        // Draw "Quit" text border
        _renderer.SetDrawColor(255, 255, 255, 255);
        _renderer.DrawRect(quitRect);

        // Draw "QUIT" text
        DrawTextQuit(quitRect);
    }

    private void DrawTextPause(Rectangle<int> container)
    {
        _renderer.SetDrawColor(255, 255, 255, 255);

        // Simple letter shapes using rectangles - improved readability
        int letterWidth = 15;
        int letterHeight = 25;
        int spacing = 5;
        int startX = container.Origin.X + (container.Size.X - (5 * letterWidth + 4 * spacing)) / 2;
        int startY = container.Origin.Y + (container.Size.Y - letterHeight) / 2;

        // P
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY, letterWidth / 3, letterHeight / 2));

        // A
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth + spacing, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth + spacing, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth + spacing, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 2 * letterWidth + spacing - letterWidth / 3, startY, letterWidth / 3, letterHeight));

        // U
        _renderer.FillRect(new Rectangle<int>(startX + 2 * letterWidth + 2 * spacing, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX + 2 * letterWidth + 2 * spacing, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 3 * letterWidth + 2 * spacing - letterWidth / 3, startY, letterWidth / 3, letterHeight));

        // S
        _renderer.FillRect(new Rectangle<int>(startX + 3 * letterWidth + 3 * spacing, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 3 * letterWidth + 3 * spacing, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 3 * letterWidth + 3 * spacing, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 3 * letterWidth + 3 * spacing, startY, letterWidth / 3, letterHeight / 2));
        _renderer.FillRect(new Rectangle<int>(startX + 4 * letterWidth + 3 * spacing - letterWidth / 3, startY + letterHeight / 2, letterWidth / 3, letterHeight / 2));

        // E 
        _renderer.FillRect(new Rectangle<int>(startX + 4 * letterWidth + 4 * spacing, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX + 4 * letterWidth + 4 * spacing, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 4 * letterWidth + 4 * spacing, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + 4 * letterWidth + 4 * spacing, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
    }

    private void DrawTextResume(Rectangle<int> container)
    {
        _renderer.SetDrawColor(255, 255, 255, 255);

        int letterHeight = container.Size.Y * 2 / 3;
        int letterWidth = letterHeight / 2;
        int padding = letterWidth / 3;
        int startY = container.Origin.Y + (container.Size.Y - letterHeight) / 2;

        // Center the text "RESUME"
        int totalWidth = 6 * (letterWidth + padding) - padding;
        int startX = container.Origin.X + (container.Size.X - totalWidth) / 2;

        // R
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY, letterWidth / 3, letterHeight / 2));
        // Diagonal leg for R
        for (int i = 0; i < letterHeight / 2; i++)
        {
            int diagX = startX + letterWidth / 2 + i / 2;
            int diagY = startY + letterHeight / 2 + i;
            _renderer.FillRect(new Rectangle<int>(diagX, diagY, letterWidth / 3, letterHeight / 12));
        }

        // E
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));

        // S
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight / 2));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY + letterHeight / 2, letterWidth / 3, letterHeight / 2));

        // U
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY, letterWidth / 3, letterHeight));

        // M
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth / 3, startY, letterWidth / 3, letterHeight / 2));
        _renderer.FillRect(new Rectangle<int>(startX + 2 * letterWidth / 3, startY, letterWidth / 3, letterHeight / 2));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY, letterWidth / 3, letterHeight));

        // E
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight / 2 - letterHeight / 8, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
    }

    private void DrawTextQuit(Rectangle<int> container)
    {
        _renderer.SetDrawColor(255, 255, 255, 255);

        int letterHeight = container.Size.Y * 2 / 3;
        int letterWidth = letterHeight / 2;
        int padding = letterWidth / 3;
        int startY = container.Origin.Y + (container.Size.Y - letterHeight) / 2;

        // Center the text "QUIT"
        int totalWidth = 4 * (letterWidth + padding) - padding;
        int startX = container.Origin.X + (container.Size.X - totalWidth) / 2;

        // Q
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight - letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth - letterWidth / 3, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY, letterWidth / 3, letterHeight - letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth / 2, startY + letterHeight / 2, letterWidth / 2, letterHeight / 4));

        // U
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth - letterWidth / 3, startY, letterWidth / 3, letterHeight));

        // I
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth / 3, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
        _renderer.FillRect(new Rectangle<int>(startX, startY + letterHeight - letterHeight / 4, letterWidth, letterHeight / 4));

        // T
        startX += letterWidth + padding;
        _renderer.FillRect(new Rectangle<int>(startX + letterWidth / 3, startY, letterWidth / 3, letterHeight));
        _renderer.FillRect(new Rectangle<int>(startX, startY, letterWidth, letterHeight / 4));
    }

    public bool ProcessInput(Input input)
    {
        // Handle menu navigation
        if (input.IsUpPressed() && !_upPressed)
        {
            _selectedOption = Math.Max(0, _selectedOption - 1);
        }
        _upPressed = input.IsUpPressed();

        if (input.IsDownPressed() && !_downPressed)
        {
            _selectedOption = Math.Min(1, _selectedOption + 1);
        }
        _downPressed = input.IsDownPressed();

        // Handle selection with Enter key
        bool enterReleased = !input.IsEnterPressed() && _enterPressed;
        _enterPressed = input.IsEnterPressed();

        if (enterReleased)
        {
            if (_selectedOption == 0) // Resume
            {
                _shouldResume = true;
                return false; // Don't quit game, just unpause
            }
            else // Quit
            {
                return true; // Signal to quit game
            }
        }

        return false;
    }

    // Add a method to check if we should resume the game
    public bool ShouldResume()
    {
        if (_shouldResume)
        {
            _shouldResume = false; // Reset after reading
            return true;
        }
        return false;
    }
}