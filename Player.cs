using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DunkPro2D;

/// <summary>
/// Player character with position, velocity, and ground detection.
/// </summary>
public class Player
{
    // Physics constants
    public const float GRAVITY = 1200f;
    public const float JUMP_FORCE = 750f;
    private const float MOVE_FRICTION = 0.95f;
    private const float GROUND_Y_THRESHOLD = 600f;

    // State
    public float X { get; private set; }
    public float Y { get; private set; }
    public float VelocityX { get; private set; }
    public float VelocityY { get; private set; }
    public bool IsGrounded { get; private set; }

    // Appearance - BIGGER PLAYER
    public const int WIDTH = 55;
    public const int HEIGHT = 100;

    // Animation state
    private float _spinRotation = 0f;
    private bool _isSpinning = false;
    private float _runFrame = 0f;
    private float _armAngle = 0f;
    private float _legAngle = 0f;
    private bool _facingRight = true;

    // For jump tracking
    private float _jumpStartY;

    public Player(float startX, float startY)
    {
        X = startX;
        Y = startY - HEIGHT + 38; // Adjust for new height
        VelocityX = 0;
        VelocityY = 0;
        IsGrounded = true;
        _jumpStartY = Y;
    }

    public void AutoRun(float speed, float deltaSeconds, ParticleSystem particles)
    {
        float dt = deltaSeconds;
        
        // Set velocity directly for smooth auto-run
        VelocityX = speed;
        _facingRight = true;

        // Apply gravity
        VelocityY += GRAVITY * dt;

        // Update position
        X += VelocityX * dt;
        Y += VelocityY * dt;

        // Clamp to screen bounds
        X = Math.Clamp(X, 0, 1280 - WIDTH);

        // Ground collision
        if (Y >= GROUND_Y_THRESHOLD - HEIGHT + 38)
        {
            Y = GROUND_Y_THRESHOLD - HEIGHT + 38;
            VelocityY = 0;
            IsGrounded = true;
            _spinRotation = 0;
            _isSpinning = false;
        }
        else
        {
            IsGrounded = false;
        }

        // Update run animation
        _runFrame += speed * dt * 0.02f;
        _legAngle = (float)Math.Sin(_runFrame * 8) * 0.6f;
        _armAngle = (float)Math.Sin(_runFrame * 8 + Math.PI) * 0.5f;
        
        // Running dust particles occasionally
        if ((int)(_runFrame * 10) % 3 == 0)
        {
            particles.Emit(new Vector2(X + WIDTH / 2, Y + HEIGHT - 5), ParticleType.Dust);
        }
    }

    public void UpdateMovement(float moveInput, float deltaSeconds, ParticleSystem particles)
    {
        float dt = deltaSeconds;

        // Only apply friction when grounded - preserve momentum in air!
        if (IsGrounded)
        {
            VelocityX *= MOVE_FRICTION;
        }

        // Apply gravity
        VelocityY += GRAVITY * dt;

        // Update position
        X += VelocityX * dt;
        Y += VelocityY * dt;

        // Clamp to screen bounds
        X = Math.Clamp(X, 0, 1280 - WIDTH);

        // Ground collision
        if (Y >= GROUND_Y_THRESHOLD - HEIGHT + 38)
        {
            Y = GROUND_Y_THRESHOLD - HEIGHT + 38;
            VelocityY = 0;
            IsGrounded = true;
            _spinRotation = 0;
            _isSpinning = false;
        }
        else
        {
            IsGrounded = false;
        }

        // Decay animation when not moving
        _legAngle = MathHelper.Lerp(_legAngle, 0, dt * 10);
        _armAngle = MathHelper.Lerp(_armAngle, 0, dt * 10);

        // Update spin
        if (_isSpinning)
        {
            _spinRotation += dt * 15f;
            if (_spinRotation >= MathHelper.TwoPi)
            {
                _spinRotation = 0;
                _isSpinning = false;
            }
        }
    }

    public void StartSpin()
    {
        if (!_isSpinning)
        {
            _isSpinning = true;
            _spinRotation = 0;
        }
    }

    public void Jump()
    {
        if (IsGrounded)
        {
            VelocityY = -JUMP_FORCE;
            IsGrounded = false;
            _jumpStartY = Y;
        }
    }

    public void Reset(float x, float y)
    {
        X = x;
        Y = y - HEIGHT + 38;  // Adjust for player height
        VelocityX = 0;
        VelocityY = 0;
        IsGrounded = true;
        _jumpStartY = Y;
        _spinRotation = 0;
        _isSpinning = false;
        _runFrame = 0;
        _armAngle = 0;
        _legAngle = 0;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)X, (int)Y, WIDTH, HEIGHT);
    }

    public Vector2 GetCenter()
    {
        return new Vector2(X + WIDTH / 2f, Y + HEIGHT / 2f);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, MoveType currentTrick)
    {
        // Calculate visual center so feet are at ground level
        // Feet are drawn at center.Y + (22 * scale) + legLength = center.Y + 33 + 48 = center.Y + 81
        // We want feet at Y + HEIGHT (bottom of bounding box)
        // So center.Y = (Y + HEIGHT) - 81
        Vector2 center = new Vector2(X + WIDTH / 2f, Y + HEIGHT - 81);
        
        // Apply spin rotation
        float rotation = _spinRotation;
        
        // Body colors
        Color skinColor = new Color(210, 160, 120);
        Color jerseyColor = new Color(200, 50, 50);
        Color jerseyColor2 = new Color(255, 255, 255);
        Color shortsColor = new Color(200, 50, 50);
        Color shoeColor = new Color(50, 50, 50);

        // Draw shadow on ground
        float groundY = GROUND_Y_THRESHOLD + 35;
        float shadowScale = IsGrounded ? 1.0f : Math.Max(0.3f, 1.0f - (groundY - (Y + HEIGHT)) / 300f);
        sb.Draw(pixel, new Rectangle((int)(X - 5), (int)groundY, (int)(WIDTH + 10), 10), 
            ColorHelper.FromRgba(0, 0, 0, (int)(60 * shadowScale)));

        if (_isSpinning)
        {
            // Draw spinning player as blur effect
            for (int i = 3; i >= 0; i--)
            {
                float r = rotation - i * 0.3f;
                byte alpha = (byte)(255 - i * 60);
                DrawPlayerBody(sb, pixel, center, r, skinColor, jerseyColor, shortsColor, shoeColor, alpha, currentTrick);
            }
        }
        else
        {
            DrawPlayerBody(sb, pixel, center, rotation, skinColor, jerseyColor, shortsColor, shoeColor, 255, currentTrick);
        }

        // Draw jersey number (scaled for bigger player)
        if (!_isSpinning)
        {
            sb.Draw(pixel, new Rectangle((int)(X + 18), (int)(Y + 30), 18, 4), jerseyColor2);
            sb.Draw(pixel, new Rectangle((int)(X + 22), (int)(Y + 25), 4, 18), jerseyColor2);
            sb.Draw(pixel, new Rectangle((int)(X + 30), (int)(Y + 25), 4, 18), jerseyColor2);
        }
    }

    private void DrawPlayerBody(SpriteBatch sb, Texture2D pixel, Vector2 center, float rotation, 
        Color skinColor, Color jerseyColor, Color shortsColor, Color shoeColor, byte alpha, MoveType currentTrick)
    {
        float scale = 1.5f;
        
        // Colors
        Color skinShadow = new Color(180, 130, 90);
        Color skinHighlight = new Color(235, 190, 160);
        Color jerseyShadow = new Color(150, 30, 30);
        Color jerseyHighlight = new Color(255, 100, 100);
        Color shortsShadow = new Color(150, 30, 30);
        Color shoeShadow = new Color(30, 30, 30);
        Color shoeHighlight = new Color(80, 80, 90);
        Color shoeAccent = new Color(255, 50, 50);
        Color hairColor = new Color(30, 20, 10);

        // All dimensions in UNSCALED units, multiply by scale when using
        // Body layout (Y: negative=up, positive=down from center)
        //   Head: Y = -42
        //   Neck: Y = -28  
        //   Torso: Y = -20 to +10 (height 30)
        //   Shorts: Y = +10 to +25 (height 15)
        //   Hips: Y = +22
        //   Shoulders: Y = -15
        
        // Precompute torso/shorts rects (no rotated offsets)
        const float torsoWUnscaled = 30f;
        const float torsoHUnscaled = 30f;
        const float shortsWUnscaled = 28f;
        const float shortsHUnscaled = 15f;

        int torsoW = (int)(torsoWUnscaled * scale);
        int torsoH = (int)(torsoHUnscaled * scale);
        Vector2 torsoCenter = new Vector2(center.X, center.Y - 5f * scale);
        int torsoX = (int)(torsoCenter.X - torsoW / 2f);
        int torsoY = (int)(torsoCenter.Y - torsoH / 2f);
        Rectangle torsoRect = new Rectangle(torsoX, torsoY, torsoW, torsoH);

        int shortsW = (int)(shortsWUnscaled * scale);
        int shortsH = (int)(shortsHUnscaled * scale);
        Vector2 shortsCenter = new Vector2(center.X, center.Y + 17f * scale);
        int shortsX = (int)(shortsCenter.X - shortsW / 2f);
        int shortsY = (int)(shortsCenter.Y - shortsH / 2f);
        Rectangle shortsRect = new Rectangle(shortsX, shortsY, shortsW, shortsH);

        // === DRAW ORDER: Back to Front ===
        
        // 1. LEGS (behind body)
        float leftLegAngle = _legAngle;
        float rightLegAngle = -_legAngle;
        
        if (!IsGrounded && currentTrick == MoveType.BetweenLegs)
        {
            leftLegAngle = -0.8f;
            rightLegAngle = 0.8f;
        }

        float legLength = 32 * scale;
        int legThickness = (int)(10 * scale);
        
        // Hips anchored to shorts bottom
        Vector2 hipL = new Vector2(shortsRect.Left + shortsRect.Width * 0.35f, shortsRect.Bottom - 2);
        Vector2 hipR = new Vector2(shortsRect.Left + shortsRect.Width * 0.65f, shortsRect.Bottom - 2);
        
        float limbBaseRotation = 0f;
        DrawLimbShaded(sb, pixel, hipL, legLength, limbBaseRotation + leftLegAngle + (float)Math.PI / 2, legThickness, 
            new Color(skinColor, alpha), new Color(skinShadow, alpha), new Color(skinHighlight, alpha));
        DrawLimbShaded(sb, pixel, hipR, legLength, limbBaseRotation + rightLegAngle + (float)Math.PI / 2, legThickness, 
            new Color(skinColor, alpha), new Color(skinShadow, alpha), new Color(skinHighlight, alpha));

        // SHOES at end of legs
        Vector2 footL = hipL + new Vector2((float)Math.Cos(limbBaseRotation + leftLegAngle + (float)Math.PI / 2) * legLength,
                           (float)Math.Sin(limbBaseRotation + leftLegAngle + (float)Math.PI / 2) * legLength);
        Vector2 footR = hipR + new Vector2((float)Math.Cos(limbBaseRotation + rightLegAngle + (float)Math.PI / 2) * legLength,
                           (float)Math.Sin(limbBaseRotation + rightLegAngle + (float)Math.PI / 2) * legLength);
        
        DrawShoe(sb, pixel, footL, scale, shoeColor, shoeShadow, shoeHighlight, shoeAccent, alpha);
        DrawShoe(sb, pixel, footR, scale, shoeColor, shoeShadow, shoeHighlight, shoeAccent, alpha);

        // 2. SHORTS (Y = +10 to +25)
        sb.Draw(pixel, shortsRect, new Color(shortsColor, alpha));
        sb.Draw(pixel, new Rectangle(shortsRect.X + shortsRect.Width / 2 - 2, shortsRect.Y, 4, shortsRect.Height), new Color(shortsShadow, (byte)(alpha * 0.4f)));
        sb.Draw(pixel, new Rectangle(shortsRect.X, shortsRect.Y + shortsRect.Height - 3, shortsRect.Width, 2), new Color(Color.White, (byte)(alpha * 0.7f)));

        // 3. TORSO (Y = -20 to +10)
        sb.Draw(pixel, torsoRect, new Color(jerseyColor, alpha));
        sb.Draw(pixel, new Rectangle(torsoRect.X, torsoRect.Y, 5, torsoRect.Height), new Color(jerseyShadow, (byte)(alpha * 0.5f)));
        sb.Draw(pixel, new Rectangle(torsoRect.Right - 5, torsoRect.Y, 5, torsoRect.Height), new Color(jerseyShadow, (byte)(alpha * 0.5f)));
        sb.Draw(pixel, new Rectangle(torsoRect.X, torsoRect.Y + 2, torsoRect.Width, 2), new Color(Color.White, (byte)(alpha * 0.8f)));
        sb.Draw(pixel, new Rectangle(torsoRect.X, torsoRect.Y + torsoRect.Height - 4, torsoRect.Width, 2), new Color(Color.White, (byte)(alpha * 0.8f)));
        DrawJerseyNumber(sb, pixel, torsoRect.X + torsoRect.Width / 2, torsoRect.Y + torsoRect.Height / 2 - 3, alpha);
        
        // Neck (Y = -28)
        Vector2 neckPos = new Vector2(center.X, center.Y - 22f * scale);
        sb.Draw(pixel, new Rectangle((int)(neckPos.X - 5), (int)(neckPos.Y), 10, (int)(8 * scale)), new Color(skinColor, alpha));

        // 4. HEAD (Y = -42)
        const float headSizeUnscaled = 22f;
        int headSize = (int)(headSizeUnscaled * scale);
        Vector2 headCenter = new Vector2(center.X, center.Y - 35f * scale);
        int headX = (int)(headCenter.X - headSize/2);
        int headY = (int)(headCenter.Y - headSize/2);
        
        // Hair
        sb.Draw(pixel, new Rectangle(headX - 2, headY - 3, headSize + 4, (int)(headSize * 0.5f)), new Color(hairColor, alpha));
        // Head
        sb.Draw(pixel, new Rectangle(headX, headY, headSize, headSize), new Color(skinColor, alpha));
        sb.Draw(pixel, new Rectangle(headX, headY, headSize/3, headSize), new Color(skinShadow, (byte)(alpha * 0.5f)));
        // Eyes
        sb.Draw(pixel, new Rectangle(headX + headSize/4, headY + headSize/3, 3, 3), new Color(Color.White, alpha));
        sb.Draw(pixel, new Rectangle(headX + headSize*2/3 - 1, headY + headSize/3, 3, 3), new Color(Color.White, alpha));
        sb.Draw(pixel, new Rectangle(headX + headSize/4 + 1, headY + headSize/3 + 1, 2, 2), new Color(Color.Black, alpha));
        sb.Draw(pixel, new Rectangle(headX + headSize*2/3, headY + headSize/3 + 1, 2, 2), new Color(Color.Black, alpha));
        // Headband
        sb.Draw(pixel, new Rectangle(headX - 2, headY + 2, headSize + 4, 6), new Color(jerseyColor, alpha));

        // 5. ARMS (in front, shoulders at Y = -15)
        float leftArmAngle = _armAngle;
        float rightArmAngle = -_armAngle;
        
        if (!IsGrounded)
        {
            switch (currentTrick)
            {
                case MoveType.HandUp:
                    leftArmAngle = -2.5f;
                    rightArmAngle = -2.5f;
                    break;
                case MoveType.HandDown:
                    leftArmAngle = 0.5f;
                    rightArmAngle = 0.5f;
                    break;
                case MoveType.BetweenLegs:
                    leftArmAngle = 1.0f;
                    rightArmAngle = 1.0f;
                    break;
                default:
                    rightArmAngle = -2.0f;
                    leftArmAngle = -0.5f;
                    break;
            }
        }

        float armLength = 25 * scale;
        int armThickness = (int)(8 * scale);
        
        // Shoulders anchored to torso rect
        Vector2 shoulderL = new Vector2(torsoRect.Left + torsoRect.Width * 0.2f, torsoRect.Top + 6);
        Vector2 shoulderR = new Vector2(torsoRect.Right - torsoRect.Width * 0.2f, torsoRect.Top + 6);
        
        DrawLimbShaded(sb, pixel, shoulderL, armLength, limbBaseRotation + leftArmAngle + (float)Math.PI / 2, armThickness, 
            new Color(skinColor, alpha), new Color(skinShadow, alpha), new Color(skinHighlight, alpha));
        DrawLimbShaded(sb, pixel, shoulderR, armLength, limbBaseRotation + rightArmAngle + (float)Math.PI / 2, armThickness, 
            new Color(skinColor, alpha), new Color(skinShadow, alpha), new Color(skinHighlight, alpha));
        
        // Hands
        Vector2 handL = shoulderL + new Vector2((float)Math.Cos(limbBaseRotation + leftArmAngle + (float)Math.PI / 2) * armLength,
                            (float)Math.Sin(limbBaseRotation + leftArmAngle + (float)Math.PI / 2) * armLength);
        Vector2 handR = shoulderR + new Vector2((float)Math.Cos(limbBaseRotation + rightArmAngle + (float)Math.PI / 2) * armLength,
                            (float)Math.Sin(limbBaseRotation + rightArmAngle + (float)Math.PI / 2) * armLength);
        sb.Draw(pixel, new Rectangle((int)(handL.X - 4), (int)(handL.Y - 4), 8, 8), new Color(skinColor, alpha));
        sb.Draw(pixel, new Rectangle((int)(handR.X - 4), (int)(handR.Y - 4), 8, 8), new Color(skinColor, alpha));

        // 6. BASKETBALL
        if (!IsGrounded)
        {
            Vector2 ballPos = handR + new Vector2(6, 0);
            DrawBall(sb, pixel, ballPos, 14 * scale);
        }
    }

    private void DrawJerseyNumber(SpriteBatch sb, Texture2D pixel, int cx, int cy, byte alpha)
    {
        Color numColor = new Color((byte)255, (byte)255, (byte)255, alpha);
        // Draw "23"
        // 2
        sb.Draw(pixel, new Rectangle(cx - 14, cy - 8, 8, 2), numColor);
        sb.Draw(pixel, new Rectangle(cx - 8, cy - 8, 2, 8), numColor);
        sb.Draw(pixel, new Rectangle(cx - 14, cy - 2, 8, 2), numColor);
        sb.Draw(pixel, new Rectangle(cx - 14, cy, 2, 8), numColor);
        sb.Draw(pixel, new Rectangle(cx - 14, cy + 6, 8, 2), numColor);
        // 3
        sb.Draw(pixel, new Rectangle(cx + 2, cy - 8, 8, 2), numColor);
        sb.Draw(pixel, new Rectangle(cx + 8, cy - 8, 2, 8), numColor);
        sb.Draw(pixel, new Rectangle(cx + 2, cy - 2, 8, 2), numColor);
        sb.Draw(pixel, new Rectangle(cx + 8, cy, 2, 8), numColor);
        sb.Draw(pixel, new Rectangle(cx + 2, cy + 6, 8, 2), numColor);
    }

    private void DrawShoe(SpriteBatch sb, Texture2D pixel, Vector2 pos, float scale, Color baseColor, Color shadow, Color highlight, Color accent, byte alpha)
    {
        int w = (int)(20 * scale);
        int h = (int)(12 * scale);
        int x = (int)(pos.X - w/2 + 4);
        int y = (int)(pos.Y - 4);
        
        // Shoe sole
        sb.Draw(pixel, new Rectangle(x - 2, y + h - 3, w + 4, 4), new Color((byte)20, (byte)20, (byte)20, alpha));
        
        // Shoe base
        sb.Draw(pixel, new Rectangle(x, y, w, h), new Color(baseColor, alpha));
        
        // Shoe shadow
        sb.Draw(pixel, new Rectangle(x, y + h - 4, w, 3), new Color(shadow, alpha));
        
        // Shoe highlight (top)
        sb.Draw(pixel, new Rectangle(x + 2, y + 1, w - 4, 2), new Color(highlight, alpha));
        
        // Shoe accent stripe
        sb.Draw(pixel, new Rectangle(x + 4, y + 3, w - 10, 3), new Color(accent, alpha));
        
        // Shoe laces area
        sb.Draw(pixel, new Rectangle(x + w/3, y, w/3, h/2), new Color(highlight, (byte)(alpha * 0.5f)));
        // Laces
        for (int i = 0; i < 3; i++)
        {
            sb.Draw(pixel, new Rectangle(x + w/3 + 2, y + 2 + i * 3, w/3 - 4, 1), new Color(Color.White, alpha));
        }
    }

    private void DrawLimb(SpriteBatch sb, Texture2D pixel, Vector2 start, float length, float angle, int thickness, Color color)
    {
        Rectangle rect = new Rectangle((int)(start.X - thickness / 2f), (int)start.Y, (int)length, thickness);
        sb.Draw(pixel, rect, null, color, angle, new Vector2(0, thickness / 2), SpriteEffects.None, 0);
    }

    private void DrawLimbShaded(SpriteBatch sb, Texture2D pixel, Vector2 start, float length, float angle, int thickness, Color baseColor, Color shadow, Color highlight)
    {
        // Simple single limb - no shading to avoid doubling
        Rectangle rect = new Rectangle((int)(start.X - thickness / 2f), (int)start.Y, (int)length, thickness);
        sb.Draw(pixel, rect, null, baseColor, angle, new Vector2(0, thickness / 2), SpriteEffects.None, 0);
    }

    private void DrawBall(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius)
    {
        // Ball shadow on ground (if close)
        float groundY = GROUND_Y_THRESHOLD + 35;
        if (center.Y < groundY - 20)
        {
            float shadowScale = Math.Max(0.3f, 1.0f - (groundY - center.Y) / 200f);
            DrawCircle(sb, pixel, new Vector2(center.X, groundY + 3), radius * shadowScale * 0.8f, 
                ColorHelper.FromRgba(0, 0, 0, (int)(40 * shadowScale)));
        }

        int r = (int)radius;
        
        // Draw ball as a filled circle
        // Outer dark edge
        DrawCircle(sb, pixel, center, radius + 1, new Color(150, 60, 0));
        
        // Main ball 
        DrawCircle(sb, pixel, center, radius, new Color(255, 120, 30));
        
        // Highlight (upper-right area) - smaller circle offset
        DrawCircle(sb, pixel, new Vector2(center.X + radius * 0.25f, center.Y - radius * 0.25f), radius * 0.5f, 
            new Color(255, 180, 100, 120));
        DrawCircle(sb, pixel, new Vector2(center.X + radius * 0.3f, center.Y - radius * 0.3f), radius * 0.25f, 
            new Color(255, 220, 180, 80));
        
        // Ball seams (black lines)
        Color seamColor = new Color(60, 30, 10);
        int seamThick = Math.Max(2, (int)(radius / 8));
        
        // Vertical seam
        sb.Draw(pixel, new Rectangle((int)(center.X - seamThick/2), (int)(center.Y - radius + 3), seamThick, (int)(radius * 2 - 6)), seamColor);
        
        // Horizontal seam  
        sb.Draw(pixel, new Rectangle((int)(center.X - radius + 3), (int)(center.Y - seamThick/2), (int)(radius * 2 - 6), seamThick), seamColor);
    }

    private void DrawCircle(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, Color color)
    {
        // Draw a circle using multiple rectangles (approximation)
        int r = (int)radius;
        if (r < 2) r = 2;
        
        // Draw filled circle row by row
        for (int y = -r; y <= r; y++)
        {
            // Calculate width at this y position using circle equation
            float width = (float)Math.Sqrt(r * r - y * y);
            int w = (int)(width * 2);
            if (w > 0)
            {
                sb.Draw(pixel, new Rectangle((int)(center.X - width), (int)(center.Y + y), w, 1), color);
            }
        }
    }
}
