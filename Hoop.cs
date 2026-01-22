using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DunkPro2D;

/// <summary>
/// Hoop/backboard on the right side. Manages dunk zone detection.
/// </summary>
public class Hoop
{
    private const int RIM_WIDTH = 90;
    private const int RIM_HEIGHT = 15;
    private const int BACKBOARD_WIDTH = 20;
    private const int BACKBOARD_HEIGHT = 140;

    // Dunk zone: a bigger rectangle around the rim for easier dunks
    public const int DUNK_ZONE_WIDTH = 180;
    public const int DUNK_ZONE_HEIGHT = 200;
    public const float DUNK_ZONE_TOP_OFFSET = -180f; // How far above rim dunk zone starts

    public float CenterX { get; }
    public float CenterY { get; }

    public Rectangle RimRect { get; }
    public Rectangle BackboardRect { get; }
    public Rectangle DunkZone { get; }

    // Net animation
    private float _netWave = 0f;

    public Hoop(float centerX, float centerY)
    {
        CenterX = centerX;
        CenterY = centerY;

        RimRect = new Rectangle(
            (int)(centerX - RIM_WIDTH / 2),
            (int)(centerY - RIM_HEIGHT / 2),
            RIM_WIDTH,
            RIM_HEIGHT
        );

        BackboardRect = new Rectangle(
            (int)(centerX + RIM_WIDTH / 2 - 10),
            (int)(centerY - BACKBOARD_HEIGHT / 2 - 20),
            BACKBOARD_WIDTH,
            BACKBOARD_HEIGHT
        );

        DunkZone = new Rectangle(
            (int)(centerX - DUNK_ZONE_WIDTH / 2),
            (int)(centerY + DUNK_ZONE_TOP_OFFSET),
            DUNK_ZONE_WIDTH,
            DUNK_ZONE_HEIGHT
        );
    }

    public bool IsPlayerInDunkZone(Player player)
    {
        Rectangle playerBounds = player.GetBounds();
        return DunkZone.Intersects(playerBounds);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, bool isDunking)
    {
        // Update net wave
        _netWave += 0.05f;
        if (isDunking) _netWave += 0.3f;

        // POLE/SUPPORT - More detailed with gradient and padding
        int poleX = (int)(CenterX + RIM_WIDTH / 2 + 5);
        int poleY = (int)CenterY - 30;
        int poleW = 18;
        int poleH = 420;
        
        // Pole shadow
        sb.Draw(pixel, new Rectangle(poleX + poleW, poleY + 20, 8, poleH - 20), ColorHelper.FromRgba(0, 0, 0, 40));
        
        // Main pole
        sb.Draw(pixel, new Rectangle(poleX, poleY, poleW, poleH), new Color(70, 70, 80));
        // Pole highlight (left edge)
        sb.Draw(pixel, new Rectangle(poleX, poleY, 4, poleH), new Color(100, 100, 115));
        // Pole shadow (right edge)  
        sb.Draw(pixel, new Rectangle(poleX + poleW - 4, poleY, 4, poleH), new Color(50, 50, 60));
        // Pole padding at base
        sb.Draw(pixel, new Rectangle(poleX - 5, poleY + poleH - 80, poleW + 10, 80), new Color(60, 60, 70));
        sb.Draw(pixel, new Rectangle(poleX - 5, poleY + poleH - 80, 3, 80), new Color(80, 80, 90));
        
        // Support arm connecting pole to backboard
        int armY = (int)CenterY - 50;
        sb.Draw(pixel, new Rectangle(poleX - 30, armY, 35, 12), new Color(80, 80, 90));
        sb.Draw(pixel, new Rectangle(poleX - 30, armY, 35, 3), new Color(100, 100, 115));
        // Diagonal support
        for (int i = 0; i < 20; i++)
        {
            sb.Draw(pixel, new Rectangle(poleX - 25 + i, armY + 12 + i, 8, 3), new Color(70, 70, 80));
        }

        // BACKBOARD - Glass effect with frame
        int bbX = BackboardRect.X;
        int bbY = BackboardRect.Y;
        int bbW = BackboardRect.Width + 8;
        int bbH = BackboardRect.Height;
        
        // Backboard shadow
        sb.Draw(pixel, new Rectangle(bbX + 5, bbY + 5, bbW, bbH), ColorHelper.FromRgba(0, 0, 0, 50));
        
        // Outer frame (thick)
        sb.Draw(pixel, new Rectangle(bbX - 6, bbY - 6, bbW + 12, bbH + 12), new Color(45, 45, 55));
        
        // Inner frame
        sb.Draw(pixel, new Rectangle(bbX - 3, bbY - 3, bbW + 6, bbH + 6), new Color(60, 60, 70));
        
        // Glass backboard with gradient
        sb.Draw(pixel, new Rectangle(bbX, bbY, bbW, bbH), new Color(180, 210, 240, 200));
        // Glass highlight (top)
        sb.Draw(pixel, new Rectangle(bbX, bbY, bbW, bbH / 4), new Color(220, 240, 255, 100));
        // Glass reflection line
        sb.Draw(pixel, new Rectangle(bbX + 3, bbY + 10, 3, bbH - 20), new Color(255, 255, 255, 80));
        
        // Target square on backboard
        int sqW = 55;
        int sqH = 40;
        int sqX = bbX - sqW - 8;
        int sqY = (int)(CenterY - sqH / 2 - 15);
        
        // Square outline (white with red inner)
        // Outer white
        sb.Draw(pixel, new Rectangle(sqX, sqY, sqW, 4), Color.White);
        sb.Draw(pixel, new Rectangle(sqX, sqY + sqH - 4, sqW, 4), Color.White);
        sb.Draw(pixel, new Rectangle(sqX, sqY, 4, sqH), Color.White);
        sb.Draw(pixel, new Rectangle(sqX + sqW - 4, sqY, 4, sqH), Color.White);
        // Inner red
        sb.Draw(pixel, new Rectangle(sqX + 6, sqY + 6, sqW - 12, 3), new Color(255, 80, 80));
        sb.Draw(pixel, new Rectangle(sqX + 6, sqY + sqH - 9, sqW - 12, 3), new Color(255, 80, 80));
        sb.Draw(pixel, new Rectangle(sqX + 6, sqY + 6, 3, sqH - 12), new Color(255, 80, 80));
        sb.Draw(pixel, new Rectangle(sqX + sqW - 9, sqY + 6, 3, sqH - 12), new Color(255, 80, 80));

        // RIM - Much more detailed
        int rimY = (int)(CenterY - 5);
        int rimX = (int)(CenterX - RIM_WIDTH / 2 + 5);
        int rimW = RIM_WIDTH - 15;
        
        // Rim connector/bracket to backboard
        sb.Draw(pixel, new Rectangle(rimX + rimW - 5, rimY - 12, 25, 20), new Color(90, 90, 100));
        sb.Draw(pixel, new Rectangle(rimX + rimW, rimY - 10, 20, 4), new Color(110, 110, 120));
        // Bolts
        sb.Draw(pixel, new Rectangle(rimX + rimW + 5, rimY - 6, 4, 4), new Color(60, 60, 70));
        sb.Draw(pixel, new Rectangle(rimX + rimW + 12, rimY - 6, 4, 4), new Color(60, 60, 70));
        
        // Rim cylinder effect (3D look)
        // Bottom of rim
        sb.Draw(pixel, new Rectangle(rimX, rimY + 6, rimW, 5), new Color(180, 60, 0));
        // Main rim top
        sb.Draw(pixel, new Rectangle(rimX, rimY, rimW, 7), new Color(255, 90, 0));
        // Rim highlight
        sb.Draw(pixel, new Rectangle(rimX, rimY, rimW, 2), new Color(255, 150, 50));
        // Rim inner edge (darker)
        sb.Draw(pixel, new Rectangle(rimX + 3, rimY + 3, rimW - 6, 3), new Color(200, 70, 0));
        
        // Rim front lip
        sb.Draw(pixel, new Rectangle(rimX - 4, rimY, 8, 10), new Color(255, 100, 0));
        sb.Draw(pixel, new Rectangle(rimX - 4, rimY, 8, 3), new Color(255, 160, 60));
        
        // Net hooks on rim
        for (int i = 0; i < 6; i++)
        {
            int hookX = rimX + 8 + i * (rimW - 20) / 5;
            sb.Draw(pixel, new Rectangle(hookX, rimY + 7, 4, 5), new Color(200, 200, 210));
        }

        // NET - Enhanced with better wave physics
        int netSegments = 12;
        int netHeight = 70;
        
        for (int i = 0; i < netSegments; i++)
        {
            float t = (float)i / (netSegments - 1);
            int startX = rimX + 5 + (int)(t * (rimW - 15));
            
            // Each net string
            for (int j = 0; j < netHeight; j += 3)
            {
                float wave = (float)Math.Sin(_netWave * 1.5f + i * 0.4f + j * 0.12f) * (2 + j * 0.08f);
                float narrowing = 1.0f - (float)j / netHeight * 0.65f;
                int x = startX + (int)(wave * narrowing) + (int)((0.5f - t) * j * 0.35f);
                
                // Net string with slight gradient
                int alpha = Math.Max(50, 220 - j * 2);
                Color netColor = ColorHelper.FromRgba(255, 255, 255, alpha);
                sb.Draw(pixel, new Rectangle(x, rimY + 11 + j, 2, 4), netColor);
                
                // Slight shadow
                if (i > 0 && i < netSegments - 1)
                {
                    sb.Draw(pixel, new Rectangle(x + 1, rimY + 12 + j, 1, 3), ColorHelper.FromRgba(200, 200, 200, alpha / 2));
                }
            }
        }

        // Cross strings of net (horizontal loops)
        for (int j = 8; j < netHeight; j += 10)
        {
            float wave = (float)Math.Sin(_netWave + j * 0.15f) * 2;
            float narrowing = 1.0f - (float)j / netHeight * 0.65f;
            int width = (int)((rimW - 15) * narrowing);
            int x = rimX + 5 + (int)((rimW - 15 - width) / 2) + (int)wave;
            int alpha = Math.Max(30, 180 - j * 2);
            sb.Draw(pixel, new Rectangle(x, rimY + 11 + j, width, 2), ColorHelper.FromRgba(255, 255, 255, alpha));
        }
        
        // Net bottom gather point
        float gatherWave = (float)Math.Sin(_netWave * 0.8f) * 3;
        int gatherX = rimX + rimW / 2 - 5 + (int)gatherWave;
        sb.Draw(pixel, new Rectangle(gatherX, rimY + netHeight + 5, 10, 8), ColorHelper.FromRgba(240, 240, 240, 100));

        // Dunk zone indicator (subtle pulsing)
        if (!isDunking)
        {
            float pulse = (float)Math.Sin(_netWave * 2) * 0.5f + 0.5f;
            int zoneAlpha = (int)(15 + pulse * 15);
            
            // Gradient zone (stronger near rim)
            for (int i = 0; i < 4; i++)
            {
                Rectangle zoneSlice = new Rectangle(
                    DunkZone.X + i * 10,
                    DunkZone.Y + i * 15,
                    DunkZone.Width - i * 20,
                    DunkZone.Height - i * 30
                );
                sb.Draw(pixel, zoneSlice, ColorHelper.FromRgba(100, 255, 100, zoneAlpha - i * 3));
            }
        }
    }
}
