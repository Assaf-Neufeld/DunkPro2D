using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DunkPro2D;

/// <summary>
/// Crowd system for background ambiance
/// </summary>
public class CrowdSystem
{
    private List<CrowdMember> _crowd = new();
    private Random _random = new();
    private float _waveOffset = 0f;

    public CrowdSystem(int screenWidth, float groundY)
    {
        // Generate crowd in tiers
        for (int tier = 0; tier < 3; tier++)
        {
            int y = (int)(groundY + 80 + tier * 35);
            int count = 30 - tier * 5;
            
            for (int i = 0; i < count; i++)
            {
                float x = (float)(_random.NextDouble() * screenWidth);
                _crowd.Add(new CrowdMember
                {
                    X = x,
                    Y = y,
                    Color = new Color(
                        (byte)(60 + _random.Next(60)),
                        (byte)(60 + _random.Next(60)),
                        (byte)(80 + _random.Next(80))
                    ),
                    Phase = (float)_random.NextDouble() * MathHelper.TwoPi,
                    Height = 12 + _random.Next(8),
                    Excitement = 0
                });
            }
        }
    }

    public void Update(float deltaTime, bool exciting)
    {
        _waveOffset += deltaTime * 3f;
        
        foreach (var member in _crowd)
        {
            member.Excitement = MathHelper.Lerp(member.Excitement, exciting ? 1f : 0f, deltaTime * 3f);
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        foreach (var member in _crowd)
        {
            float wave = (float)Math.Sin(_waveOffset + member.Phase) * member.Excitement * 8;
            
            // Body
            Rectangle body = new Rectangle((int)member.X, (int)(member.Y - wave), 10, member.Height);
            sb.Draw(pixel, body, member.Color);
            
            // Head
            Rectangle head = new Rectangle((int)(member.X + 2), (int)(member.Y - 10 - wave), 6, 8);
            sb.Draw(pixel, head, new Color(210, 170, 130));

            // Arms up when excited
            if (member.Excitement > 0.3f)
            {
                int armHeight = (int)(member.Excitement * 10);
                sb.Draw(pixel, new Rectangle((int)member.X - 2, (int)(member.Y - armHeight - wave), 3, armHeight), member.Color);
                sb.Draw(pixel, new Rectangle((int)member.X + 10, (int)(member.Y - armHeight - wave), 3, armHeight), member.Color);
            }
        }
    }
}

public class CrowdMember
{
    public float X, Y;
    public Color Color;
    public float Phase;
    public int Height;
    public float Excitement;
}
