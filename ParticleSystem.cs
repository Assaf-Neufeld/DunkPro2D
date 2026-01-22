using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DunkPro2D;

/// <summary>
/// Particle types for visual effects
/// </summary>
public enum ParticleType
{
    Dust,
    TrickSpark,
    SpinTrail,
    DunkFlash,
    Confetti
}

/// <summary>
/// Individual particle data
/// </summary>
public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public Color Color;
    public float Size;
    public float Rotation;
    public float RotationSpeed;
    public ParticleType Type;
    public bool InFront;
}

/// <summary>
/// Particle system for visual effects
/// </summary>
public class ParticleSystem
{
    private List<Particle> _particles = new();
    private Random _random = new();

    public void Emit(Vector2 position, ParticleType type)
    {
        Particle p = new Particle
        {
            Position = position,
            Type = type,
            Rotation = (float)(_random.NextDouble() * Math.PI * 2),
            RotationSpeed = (float)(_random.NextDouble() - 0.5) * 5f
        };

        switch (type)
        {
            case ParticleType.Dust:
                p.Velocity = new Vector2((float)(_random.NextDouble() - 0.5) * 50, (float)(_random.NextDouble() * -30 - 20));
                p.Life = p.MaxLife = 0.4f + (float)_random.NextDouble() * 0.2f;
                p.Color = new Color(180, 140, 100);
                p.Size = 4 + (float)_random.NextDouble() * 4;
                p.InFront = false;
                break;

            case ParticleType.TrickSpark:
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                float speed = 80 + (float)_random.NextDouble() * 80;
                p.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                p.Life = p.MaxLife = 0.3f + (float)_random.NextDouble() * 0.2f;
                p.Color = new Color(255, 200, 50);
                p.Size = 3 + (float)_random.NextDouble() * 3;
                p.InFront = true;
                break;

            case ParticleType.SpinTrail:
                p.Velocity = new Vector2((float)(_random.NextDouble() - 0.5) * 30, (float)(_random.NextDouble() - 0.5) * 30);
                p.Life = p.MaxLife = 0.5f + (float)_random.NextDouble() * 0.3f;
                p.Color = new Color(100, 200, 255);
                p.Size = 6 + (float)_random.NextDouble() * 6;
                p.InFront = false;
                break;

            case ParticleType.DunkFlash:
                angle = (float)(_random.NextDouble() * Math.PI * 2);
                speed = 150 + (float)_random.NextDouble() * 150;
                p.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                p.Life = p.MaxLife = 0.6f + (float)_random.NextDouble() * 0.4f;
                p.Color = _random.NextDouble() > 0.5 ? new Color(255, 200, 50) : new Color(255, 100, 50);
                p.Size = 8 + (float)_random.NextDouble() * 12;
                p.InFront = true;
                break;

            case ParticleType.Confetti:
                angle = (float)(_random.NextDouble() * Math.PI * 2);
                speed = 200 + (float)_random.NextDouble() * 300;
                p.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed - 100);
                p.Life = p.MaxLife = 1.5f + (float)_random.NextDouble() * 1f;
                p.Color = new Color(
                    (byte)_random.Next(150, 255),
                    (byte)_random.Next(150, 255),
                    (byte)_random.Next(150, 255)
                );
                p.Size = 6 + (float)_random.NextDouble() * 8;
                p.RotationSpeed = (float)(_random.NextDouble() - 0.5) * 15f;
                p.InFront = true;
                break;
        }

        _particles.Add(p);
    }

    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life -= deltaTime;
            
            if (p.Life <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }

            p.Position += p.Velocity * deltaTime;
            p.Rotation += p.RotationSpeed * deltaTime;

            // Gravity for confetti
            if (p.Type == ParticleType.Confetti)
            {
                p.Velocity.Y += 400 * deltaTime;
                p.Velocity.X *= 0.98f;
            }
            else if (p.Type == ParticleType.Dust)
            {
                p.Velocity.Y += 100 * deltaTime;
            }

            // Friction
            p.Velocity *= 0.98f;
        }
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, bool behindPlayer)
    {
        foreach (var p in _particles)
        {
            if (p.InFront == behindPlayer) continue;

            float lifeRatio = p.Life / p.MaxLife;
            byte alpha = (byte)(255 * lifeRatio);
            Color color = new Color(p.Color.R, p.Color.G, p.Color.B, alpha);
            
            float size = p.Size * (0.5f + lifeRatio * 0.5f);
            
            Rectangle rect = new Rectangle(
                (int)(p.Position.X - size / 2),
                (int)(p.Position.Y - size / 2),
                (int)size,
                (int)size
            );

            sb.Draw(pixel, rect, null, color, p.Rotation, Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}
