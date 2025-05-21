using System;
using System.Collections.Generic;

namespace TheAdventure.Models;

public class WaveUI
{
    private readonly GameRenderer _renderer;
    private readonly WaveSystem _waveSystem;
    private DateTimeOffset? _buffMessageTime;
    private string? _buffMessage;
    private const double BuffMessageDuration = 3.5; // seconds

    public WaveUI(GameRenderer renderer, WaveSystem waveSystem)
    {
        _renderer = renderer;
        _waveSystem = waveSystem;
    }

    public void ShowBuffMessage(PlayerBuffType buffType)
    {
        var buff = PlayerBuff.CreateBuff(buffType);
        _buffMessage = $"WAVE COMPLETED! BUFF: {buff.Description}";
        _buffMessageTime = DateTimeOffset.Now;
    }

    public void ShowWaveStartMessage(int waveNumber)
    {
        _buffMessage = $"WAVE {waveNumber} STARTED!";
        _buffMessageTime = DateTimeOffset.Now;
    }

    public void Render()
    {
        // Draw wave number at the top-right corner
        var waveText = $"WAVE: {_waveSystem.CurrentWave}";
        var enemiesText = $"ENEMIES: {_waveSystem.EnemiesRemainingInWave}";

        // Get screen dimensions
        int screenWidth = _renderer.GetScreenWidth();
        int screenHeight = _renderer.GetScreenHeight();

        int charWidth = 10; // Bitmap font width + spacing
        int charHeight = 12; // Bitmap font height
        int padding = 8;
        int infoBoxWidth = Math.Max(waveText.Length, enemiesText.Length) * charWidth + padding * 2;
        int infoBoxHeight = charHeight * 2 + padding * 3;
        int infoBoxX = screenWidth - infoBoxWidth - 20;
        int infoBoxY = 20;
        _renderer.SetDrawColor(0, 0, 0, 180);
        for (int py = infoBoxY; py < infoBoxY + infoBoxHeight; py++)
            for (int px = infoBoxX; px < infoBoxX + infoBoxWidth; px++)
                _renderer.DrawUIPoint(px, py);

        int infoTextY = infoBoxY + padding;
        _renderer.RenderAsciiText(waveText, infoBoxX + padding, infoTextY, 255, 60, 60); // Red
        infoTextY += charHeight + 4;
        _renderer.RenderAsciiText(enemiesText, infoBoxX + padding, infoTextY, 255, 60, 60); // Red

        // Show buff message if active
        if (_buffMessageTime.HasValue &&
            (DateTimeOffset.Now - _buffMessageTime.Value).TotalSeconds < BuffMessageDuration)
        {
            // Calculate position at center of screen
            int textWidth = _buffMessage!.Length * charWidth;
            int textX = (screenWidth - textWidth) / 2;
            int textY = 80;
            int boxWidth = textWidth + padding * 2;
            int boxHeight = charHeight + padding * 2;
            int boxX = textX - padding;
            int boxY = textY - padding;
            // Draw background box
            _renderer.SetDrawColor(0, 0, 60, 220);
            for (int py = boxY; py < boxY + boxHeight; py++)
                for (int px = boxX; px < boxX + boxWidth; px++)
                    _renderer.DrawUIPoint(px, py);
            // Draw border
            _renderer.SetDrawColor(255, 255, 255, 255);
            for (int px = boxX; px < boxX + boxWidth; px++)
            {
                _renderer.DrawUIPoint(px, boxY);
                _renderer.DrawUIPoint(px, boxY + boxHeight - 1);
            }
            for (int py = boxY; py < boxY + boxHeight; py++)
            {
                _renderer.DrawUIPoint(boxX, py);
                _renderer.DrawUIPoint(boxX + boxWidth - 1, py);
            }
            // Draw the message with a strong yellow color
            _renderer.RenderAsciiText(_buffMessage, textX, textY, 255, 255, 80);
        }
    }
}
