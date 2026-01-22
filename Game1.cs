using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DunkPro2D;

/// <summary>
/// Main game loop for Dunk Contest 2D Pro.
/// Manages game states, physics, input, scoring, and rendering.
/// </summary>
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont? _font;
    private Texture2D _pixel = null!;

    // Game state
    private GameState _gameState = GameState.Ready;
    private Player _player = null!;
    private Hoop _hoop = null!;
    private TrickTracker _trickTracker = null!;
    private ScoringEngine _scoringEngine = null!;
    private AttemptResult? _lastAttemptResult;
    private ParticleSystem _particles = null!;
    private CrowdSystem _crowd = null!;

    // Camera/View
    private const int SCREEN_WIDTH = 1280;
    private const int SCREEN_HEIGHT = 720;
    private const float GROUND_Y = 600f;
    private const float HOOP_CENTER_X = 1000f;
    private const float HOOP_CENTER_Y = 380f;

    // Timing & Slow Motion
    private double _totalGameTime = 0;
    private float _timeScale = 1.0f;
    private float _targetTimeScale = 1.0f;
    private const float SLOW_MO_SCALE = 0.25f;
    private const float TIME_SCALE_LERP_SPEED = 8f;
    private double _slowMoTimer = 0;
    private const double SLOW_MO_DURATION = 1800;
    private bool _isDunking = false;
    private Vector2 _dunkPosition;
    
    // Auto-run gameplay
    private bool _isRunning = false;
    private bool _hasJumped = false;
    private const float AUTO_RUN_SPEED = 350f;

    // Screen shake
    private float _screenShake = 0f;
    private Random _shakeRandom = new Random();

    // Instructions overlay
    private bool _showInstructions = false;
    private KeyboardState _previousKeyState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = SCREEN_WIDTH,
            PreferredBackBufferHeight = SCREEN_HEIGHT,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.Title = "Dunk Contest 2D Pro";

        _player = new Player(150f, GROUND_Y);
        _hoop = new Hoop(HOOP_CENTER_X, HOOP_CENTER_Y);
        _trickTracker = new TrickTracker();
        _scoringEngine = new ScoringEngine();
        _particles = new ParticleSystem();
        _crowd = new CrowdSystem(SCREEN_WIDTH, GROUND_Y);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        try
        {
            _font = Content.Load<SpriteFont>("Arial");
        }
        catch
        {
            _font = null;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        float realDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _totalGameTime += gameTime.ElapsedGameTime.TotalMilliseconds;

        _timeScale = MathHelper.Lerp(_timeScale, _targetTimeScale, TIME_SCALE_LERP_SPEED * realDelta);
        float scaledDelta = realDelta * _timeScale;

        KeyboardState ks = Keyboard.GetState();

        if (ks.IsKeyDown(Keys.Escape))
            Exit();

        if (ks.IsKeyDown(Keys.H) && !_previousKeyState.IsKeyDown(Keys.H))
        {
            _showInstructions = !_showInstructions;
        }

        _particles.Update(realDelta);
        _crowd.Update(realDelta, _gameState == GameState.InAttempt && !_player.IsGrounded);
        _screenShake = Math.Max(0, _screenShake - realDelta * 10f);

        if (_isDunking)
        {
            _slowMoTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_slowMoTimer >= SLOW_MO_DURATION)
            {
                _isDunking = false;
                _targetTimeScale = 1.0f;
                _screenShake = 1.5f;
                for (int i = 0; i < 50; i++)
                {
                    _particles.Emit(_dunkPosition, ParticleType.Confetti);
                }
            }
        }

        switch (_gameState)
        {
            case GameState.Ready:
                UpdateReady(ks);
                break;
            case GameState.InAttempt:
                UpdateInAttempt(ks, scaledDelta);
                break;
            case GameState.Results:
                UpdateResults(ks);
                break;
        }

        _previousKeyState = ks;
        base.Update(gameTime);
    }

    private void UpdateReady(KeyboardState ks)
    {
        if (ks.IsKeyDown(Keys.Space) && !_previousKeyState.IsKeyDown(Keys.Space))
        {
            StartAttempt();
        }
    }

    private void UpdateInAttempt(KeyboardState ks, float scaledDelta)
    {
        if (_isRunning && _player.IsGrounded && !_hasJumped)
        {
            _player.AutoRun(AUTO_RUN_SPEED, scaledDelta, _particles);
        }
        else
        {
            _player.UpdateMovement(0, scaledDelta, _particles);
        }

        if (ks.IsKeyDown(Keys.Space) && !_previousKeyState.IsKeyDown(Keys.Space) && _player.IsGrounded && !_hasJumped)
        {
            _player.Jump();
            _hasJumped = true;
            _trickTracker.ResetAttempt(_totalGameTime);
            _targetTimeScale = SLOW_MO_SCALE;
            
            for (int i = 0; i < 8; i++)
            {
                _particles.Emit(new Vector2(_player.X + 30, _player.Y + 80), ParticleType.Dust);
            }
        }

        if (!_player.IsGrounded)
        {
            if (ks.IsKeyDown(Keys.J) && !_previousKeyState.IsKeyDown(Keys.J))
            {
                if (_trickTracker.RecordMove(MoveType.HandUp, _totalGameTime))
                    _particles.Emit(_player.GetCenter(), ParticleType.TrickSpark);
            }

            if (ks.IsKeyDown(Keys.K) && !_previousKeyState.IsKeyDown(Keys.K))
            {
                if (_trickTracker.RecordMove(MoveType.HandDown, _totalGameTime))
                    _particles.Emit(_player.GetCenter(), ParticleType.TrickSpark);
            }

            if (ks.IsKeyDown(Keys.L) && !_previousKeyState.IsKeyDown(Keys.L))
            {
                if (_trickTracker.RecordMove(MoveType.BetweenLegs, _totalGameTime))
                {
                    _particles.Emit(_player.GetCenter(), ParticleType.TrickSpark);
                    _particles.Emit(_player.GetCenter(), ParticleType.TrickSpark);
                }
            }

            if (ks.IsKeyDown(Keys.I) && !_previousKeyState.IsKeyDown(Keys.I))
            {
                if (_trickTracker.RecordMove(MoveType.Spin, _totalGameTime))
                {
                    _player.StartSpin();
                    for (int i = 0; i < 5; i++)
                        _particles.Emit(_player.GetCenter(), ParticleType.SpinTrail);
                }
            }

            if (_hoop.IsPlayerInDunkZone(_player) && !_isDunking)
            {
                PerformDunk();
            }
        }

        if (_player.IsGrounded && _hasJumped && !_isDunking)
        {
            _targetTimeScale = 1.0f;
            EndAttempt(dunkSuccessful: false);
        }
    }

    private void UpdateResults(KeyboardState ks)
    {
        if (ks.IsKeyDown(Keys.Space) && !_previousKeyState.IsKeyDown(Keys.Space))
        {
            _gameState = GameState.Ready;
            _player.Reset(80f, GROUND_Y);
            _trickTracker = new TrickTracker();
            _isRunning = false;
            _hasJumped = false;
            _isDunking = false;
            _targetTimeScale = 1.0f;
        }
    }

    private void StartAttempt()
    {
        _gameState = GameState.InAttempt;
        _player.Reset(80f, GROUND_Y);
        _trickTracker = new TrickTracker();
        _trickTracker.StartAttempt(_totalGameTime);
        _isRunning = true;
        _hasJumped = false;
        _targetTimeScale = 1.0f;
    }

    private void PerformDunk()
    {
        _trickTracker.RecordMove(MoveType.Dunk, _totalGameTime);
        
        _isDunking = true;
        _slowMoTimer = 0;
        _targetTimeScale = SLOW_MO_SCALE * 0.5f;
        _dunkPosition = _player.GetCenter();
        
        for (int i = 0; i < 30; i++)
        {
            _particles.Emit(_dunkPosition, ParticleType.DunkFlash);
        }
        
        EndAttempt(dunkSuccessful: true);
    }

    private void EndAttempt(bool dunkSuccessful)
    {
        _gameState = GameState.Results;

        var attemptData = new AttemptData
        {
            Moves = _trickTracker.GetRecordedMoves(),
            DunkSuccessful = dunkSuccessful,
            MoveSequenceCount = _trickTracker.GetRecordedMoves().Count,
            HasSpin = _trickTracker.GetRecordedMoves().Any(m => m.Type == MoveType.Spin),
            TotalAirTime = _trickTracker.GetTotalAirTime(),
            DunkTiming = dunkSuccessful ? _trickTracker.GetDunkTiming() : 0
        };

        _lastAttemptResult = _scoringEngine.CalculateScore(attemptData);
    }

    protected override void Draw(GameTime gameTime)
    {
        Vector2 shakeOffset = Vector2.Zero;
        if (_screenShake > 0)
        {
            shakeOffset = new Vector2(
                (float)(_shakeRandom.NextDouble() * 2 - 1) * _screenShake * 8,
                (float)(_shakeRandom.NextDouble() * 2 - 1) * _screenShake * 8
            );
        }

        GraphicsDevice.Clear(new Color(25, 25, 40));
        
        Matrix transform = Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y, 0);
        _spriteBatch.Begin(transformMatrix: transform);

        DrawArenaBackground();
        _crowd.Draw(_spriteBatch, _pixel);
        DrawCourt();
        _hoop.Draw(_spriteBatch, _pixel, _isDunking);
        _particles.Draw(_spriteBatch, _pixel, behindPlayer: true);
        _player.Draw(_spriteBatch, _pixel, _trickTracker.GetCurrentTrick());
        _particles.Draw(_spriteBatch, _pixel, behindPlayer: false);

        if (_isDunking)
        {
            DrawSlowMoEffect();
        }

        switch (_gameState)
        {
            case GameState.Ready:
                DrawReadyScreen();
                break;
            case GameState.InAttempt:
                DrawInAttemptHUD();
                break;
            case GameState.Results:
                DrawResultsScreen();
                break;
        }

        if (_showInstructions)
        {
            DrawInstructionsOverlay();
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawArenaBackground()
    {
        for (int i = 0; i < 5; i++)
        {
            float x = 100 + i * 280;
            float glowSize = 80 + (float)Math.Sin(_totalGameTime / 500 + i) * 10;
            DrawGlow(new Vector2(x, 50), glowSize, ColorHelper.FromRgba(255, 250, 200, 30));
        }

        for (int i = 0; i < 3; i++)
        {
            Rectangle banner = new Rectangle(200 + i * 350, 30, 120, 60);
            _spriteBatch.Draw(_pixel, banner, new Color(60, 20, 80));
            _spriteBatch.Draw(_pixel, new Rectangle(banner.X + 5, banner.Y + 5, banner.Width - 10, banner.Height - 10), new Color(100, 40, 120));
        }
    }

    private void DrawCourt()
    {
        Rectangle courtRect = new Rectangle(0, (int)GROUND_Y + 40, SCREEN_WIDTH, SCREEN_HEIGHT - (int)GROUND_Y - 40);
        _spriteBatch.Draw(_pixel, courtRect, new Color(205, 133, 63));

        for (int i = 0; i < 20; i++)
        {
            int y = (int)GROUND_Y + 45 + i * 8;
            _spriteBatch.Draw(_pixel, new Rectangle(0, y, SCREEN_WIDTH, 2), ColorHelper.FromRgba(180, 110, 50, 100));
        }

        _spriteBatch.Draw(_pixel, new Rectangle(0, (int)GROUND_Y + 38, SCREEN_WIDTH, 4), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(750, (int)GROUND_Y + 38, 4, 50), ColorHelper.FromRgba(255, 255, 255, 150));
        
        Rectangle paintArea = new Rectangle(900, (int)GROUND_Y - 150, 380, 190);
        _spriteBatch.Draw(_pixel, paintArea, ColorHelper.FromRgba(139, 69, 19, 80));
        _spriteBatch.Draw(_pixel, new Rectangle(paintArea.X, paintArea.Y, paintArea.Width, 3), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(paintArea.X, paintArea.Y, 3, paintArea.Height), Color.White);
    }

    private void DrawGlow(Vector2 center, float radius, Color color)
    {
        for (int i = 5; i > 0; i--)
        {
            float r = radius * (i / 5f);
            Rectangle glowRect = new Rectangle((int)(center.X - r), (int)(center.Y - r), (int)(r * 2), (int)(r * 2));
            _spriteBatch.Draw(_pixel, glowRect, new Color(color.R, color.G, color.B, (byte)(color.A / i)));
        }
    }

    private void DrawSlowMoEffect()
    {
        float progress = (float)(_slowMoTimer / SLOW_MO_DURATION);
        int lineCount = 16;
        float lineLength = 100 + progress * 300;
        
        for (int i = 0; i < lineCount; i++)
        {
            float angle = (float)(i * Math.PI * 2 / lineCount + _totalGameTime / 200);
            Vector2 start = _dunkPosition + new Vector2((float)Math.Cos(angle) * 50, (float)Math.Sin(angle) * 50);
            Vector2 end = _dunkPosition + new Vector2((float)Math.Cos(angle) * lineLength, (float)Math.Sin(angle) * lineLength);
            
            DrawLine(start, end, ColorHelper.FromRgba(255, 200, 50, (int)(150 * (1 - progress))), 3);
        }

        if (progress < 0.7f)
        {
            float scale = 2.0f + progress * 2;
            int alpha = (int)(255 * (1 - progress / 0.7f));
            DrawText("SLAM!", (int)(_dunkPosition.X - 80 * scale / 2), (int)(_dunkPosition.Y - 150), ColorHelper.FromRgba(255, 100, 0, alpha), scale);
        }

        int vignetteAlpha = (int)(100 * (1 - progress));
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, SCREEN_WIDTH, 80), ColorHelper.FromRgba(0, 0, 0, vignetteAlpha));
        _spriteBatch.Draw(_pixel, new Rectangle(0, SCREEN_HEIGHT - 80, SCREEN_WIDTH, 80), ColorHelper.FromRgba(0, 0, 0, vignetteAlpha));
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        Rectangle rect = new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness);
        _spriteBatch.Draw(_pixel, rect, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
    }

    private void DrawReadyScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), ColorHelper.FromRgba(0, 0, 0, 150));

        float titlePulse = 1.0f + (float)Math.Sin(_totalGameTime / 300) * 0.1f;
        DrawText("DUNK CONTEST 2D PRO", SCREEN_WIDTH / 2 - (int)(220 * titlePulse), 80, new Color(255, 200, 50), 2.2f * titlePulse);
        DrawText("The Ultimate Slam Dunk Experience", SCREEN_WIDTH / 2 - 180, 150, new Color(200, 200, 200), 1.0f);

        float promptAlpha = (float)(Math.Sin(_totalGameTime / 400) + 1) / 2;
        DrawText("Press SPACE to Start Running!", SCREEN_WIDTH / 2 - 150, 250, ColorHelper.FromRgba(255, 255, 100, (int)(150 + 105 * promptAlpha)), 1.2f);

        int boxX = SCREEN_WIDTH / 2 - 220;
        int boxY = 320;
        _spriteBatch.Draw(_pixel, new Rectangle(boxX - 10, boxY - 10, 460, 200), ColorHelper.FromRgba(40, 40, 60, 200));
        
        DrawText("How to Dunk:", boxX, boxY, Color.Cyan, 1.1f);
        DrawText("1. SPACE to start running", boxX + 20, boxY + 35, Color.White, 0.9f);
        DrawText("2. SPACE again to JUMP (enters slow-mo!)", boxX + 20, boxY + 60, new Color(255, 200, 100), 0.9f);
        DrawText("3. J K L I - Do tricks in slow motion!", boxX + 20, boxY + 85, new Color(150, 255, 150), 0.9f);
        DrawText("4. Reach the hoop = AUTO DUNK!", boxX + 20, boxY + 110, new Color(255, 150, 50), 0.9f);
        DrawText("H - Full Instructions", boxX + 20, boxY + 155, new Color(150, 150, 255), 0.9f);

        DrawBasketball(new Vector2(200, 400), 40, (float)(_totalGameTime / 1000));
        DrawBasketball(new Vector2(1080, 400), 40, (float)(-_totalGameTime / 1000));
    }

    private void DrawBasketball(Vector2 center, float radius, float rotation)
    {
        Rectangle ballRect = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        _spriteBatch.Draw(_pixel, ballRect, new Color(255, 120, 20));
        
        float lineOffset = (float)Math.Sin(rotation) * 5;
        _spriteBatch.Draw(_pixel, new Rectangle((int)(center.X - 2 + lineOffset), (int)(center.Y - radius), 4, (int)(radius * 2)), new Color(50, 30, 10));
        _spriteBatch.Draw(_pixel, new Rectangle((int)(center.X - radius), (int)(center.Y - 2), (int)(radius * 2), 4), new Color(50, 30, 10));
    }

    private void DrawInstructionsOverlay()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), ColorHelper.FromRgba(0, 0, 20, 230));

        int startY = 40;
        DrawText("HOW TO PLAY DUNK CONTEST 2D PRO", SCREEN_WIDTH / 2 - 250, startY, new Color(255, 200, 50), 1.5f);
        
        int y = startY + 60;
        int leftCol = 60;
        int rightCol = 660;

        DrawText("GAMEPLAY FLOW", leftCol, y, Color.Cyan, 1.2f); y += 30;
        DrawText("1. Press SPACE to start running", leftCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("2. Press SPACE again to JUMP", leftCol + 20, y, new Color(255, 200, 100), 0.9f); y += 25;
        DrawText("   (Enters SLOW MOTION!)", leftCol + 20, y, new Color(255, 200, 100), 0.9f); y += 25;
        DrawText("3. Do your tricks while airborne", leftCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("4. Reach the hoop = AUTO DUNK!", leftCol + 20, y, new Color(255, 150, 50), 0.9f); y += 40;

        DrawText("TRICKS (while airborne)", leftCol, y, new Color(150, 255, 150), 1.2f); y += 30;
        DrawText("J - Hand Up     (+15 pts)", leftCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("K - Hand Down   (+15 pts)", leftCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("L - Between Legs (+15 pts)", leftCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("I - SPIN!       (+15 pts + bonus)", leftCol + 20, y, new Color(255, 200, 100), 0.9f); y += 40;

        DrawText("AUTO-DUNK", leftCol, y, new Color(255, 100, 50), 1.2f); y += 30;
        DrawText("When you reach the hoop, the", leftCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("dunk happens automatically!", leftCol + 20, y, new Color(180, 180, 180), 0.8f); y += 22;
        DrawText("Focus on your trick combos!", leftCol + 20, y, new Color(180, 180, 180), 0.8f);

        y = startY + 60;
        DrawText("SCORING SYSTEM", rightCol, y, Color.Yellow, 1.2f); y += 30;
        DrawText("Base Dunk:      100 pts", rightCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("Each Trick:     +15 pts", rightCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("Variety Bonus:  +20 pts per unique trick", rightCol + 20, y, new Color(100, 255, 255), 0.9f); y += 25;
        DrawText("Timing Bonus:   Up to +50 pts", rightCol + 20, y, new Color(255, 255, 100), 0.9f); y += 25;
        DrawText("Creativity:     +75 pts max", rightCol + 20, y, new Color(255, 100, 255), 0.9f); y += 40;

        DrawText("PRO TIPS", rightCol, y, new Color(100, 200, 255), 1.2f); y += 30;
        DrawText("Mix up your tricks for variety bonus!", rightCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("Do tricks mid-jump for timing bonus", rightCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("Include a SPIN for extra creativity", rightCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("Chain 3+ moves for creativity bonus", rightCol + 20, y, Color.White, 0.9f); y += 25;
        DrawText("Don't spam the same move!", rightCol + 20, y, new Color(255, 150, 150), 0.9f); y += 50;

        DrawText("THE AUTO-DUNK ZONE", SCREEN_WIDTH / 2 - 100, y, new Color(100, 255, 100), 1.2f); y += 35;
        
        int diagramX = SCREEN_WIDTH / 2 - 60;
        _spriteBatch.Draw(_pixel, new Rectangle(diagramX, y, 30, 80), new Color(200, 100, 0));
        _spriteBatch.Draw(_pixel, new Rectangle(diagramX - 40, y + 40, 50, 8), Color.Red);
        _spriteBatch.Draw(_pixel, new Rectangle(diagramX - 60, y - 20, 80, 100), ColorHelper.FromRgba(0, 255, 0, 50));
        DrawText("Green zone = Auto dunk!", diagramX + 50, y + 30, new Color(100, 255, 100), 0.9f);

        y += 120;
        DrawText("OTHER CONTROLS", SCREEN_WIDTH / 2 - 80, y, Color.Gray, 1.0f); y += 25;
        DrawText("R - Restart (on results)    ESC - Exit    H - Toggle this help", SCREEN_WIDTH / 2 - 220, y, new Color(150, 150, 150), 0.85f);

        float pulse = (float)(Math.Sin(_totalGameTime / 300) + 1) / 2;
        DrawText("Press H to close", SCREEN_WIDTH / 2 - 80, SCREEN_HEIGHT - 50, ColorHelper.FromRgba(255, 255, 100, (int)(150 + 105 * pulse)), 1.0f);
    }

    private void DrawInAttemptHUD()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, SCREEN_WIDTH, 120), ColorHelper.FromRgba(0, 0, 0, 100));

        string statusStr = _player.IsGrounded ? "GROUNDED" : "AIRBORNE";
        Color statusColor = _player.IsGrounded ? new Color(100, 200, 100) : new Color(255, 200, 50);
        DrawText(statusStr, 25, 15, statusColor, 1.1f);

        string comboStr = _trickTracker.GetComboString();
        if (comboStr != "None")
        {
            float comboScale = 1.0f + (float)Math.Sin(_totalGameTime / 100) * 0.05f;
            DrawText($"COMBO: {comboStr}", 25, 45, new Color(100, 255, 100), comboScale);
        }

        float stylePercent = _trickTracker.GetStyleMeter();
        DrawText("STYLE", 25, 80, Color.Cyan, 0.9f);
        
        int barX = 90;
        int barWidth = 200;
        int barHeight = 18;
        
        _spriteBatch.Draw(_pixel, new Rectangle(barX, 82, barWidth + 4, barHeight + 4), new Color(20, 20, 30));
        
        int fillWidth = (int)(barWidth * stylePercent / 100f);
        Color barColor = stylePercent > 75 ? new Color(255, 200, 50) : 
                         stylePercent > 50 ? new Color(50, 255, 150) : 
                         new Color(50, 200, 255);
        _spriteBatch.Draw(_pixel, new Rectangle(barX + 2, 84, fillWidth, barHeight), barColor);
        
        if (stylePercent > 50)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(barX + 2, 84, fillWidth, barHeight / 2), ColorHelper.FromRgba(255, 255, 255, 50));
        }

        DrawText($"{stylePercent:F0}%", barX + barWidth + 15, 80, barColor, 0.9f);

        int moveCount = _trickTracker.GetRecordedMoves().Count(m => m.Type != MoveType.Dunk);
        if (moveCount > 0)
        {
            DrawText($"TRICKS: {moveCount}", SCREEN_WIDTH - 150, 15, new Color(200, 150, 255), 1.0f);
        }

        if (_hoop.IsPlayerInDunkZone(_player) && !_player.IsGrounded)
        {
            float hintPulse = (float)(Math.Sin(_totalGameTime / 100) + 1) / 2;
            DrawText("AUTO DUNK!", SCREEN_WIDTH / 2 - 70, 200, ColorHelper.FromRgba(255, 100, 50, (int)(150 + 105 * hintPulse)), 1.3f);
        }

        DrawText("H - Help", SCREEN_WIDTH - 100, SCREEN_HEIGHT - 30, ColorHelper.FromRgba(150, 150, 150, 150), 0.7f);

        // Always-visible controls panel on the right side
        DrawControlsPanel();
    }

    private void DrawControlsPanel()
    {
        int panelX = SCREEN_WIDTH - 180;
        int panelY = 140;
        int panelW = 170;
        int panelH = 200;

        // Semi-transparent background
        _spriteBatch.Draw(_pixel, new Rectangle(panelX - 5, panelY - 5, panelW + 10, panelH + 10), ColorHelper.FromRgba(0, 0, 0, 150));

        DrawText("CONTROLS", panelX + 30, panelY, Color.Cyan, 0.9f);
        
        int y = panelY + 30;
        Color keyColor = new Color(255, 255, 100);
        Color descColor = new Color(200, 200, 200);
        
        // Jump instruction
        if (!_hasJumped)
        {
            DrawText("SPACE", panelX + 10, y, new Color(100, 255, 100), 0.85f);
            DrawText("Jump!", panelX + 80, y, new Color(100, 255, 100), 0.85f);
        }
        else if (!_player.IsGrounded)
        {
            DrawText("SPACE", panelX + 10, y, new Color(80, 80, 80), 0.85f);
            DrawText("(jumped)", panelX + 80, y, new Color(80, 80, 80), 0.85f);
        }
        y += 25;

        // Divider
        _spriteBatch.Draw(_pixel, new Rectangle(panelX + 10, y, panelW - 20, 1), ColorHelper.FromRgba(100, 100, 100, 150));
        y += 10;

        // Trick controls - highlight when airborne
        Color trickLabelColor = !_player.IsGrounded ? new Color(150, 255, 150) : new Color(100, 100, 100);
        DrawText("TRICKS:", panelX + 10, y, trickLabelColor, 0.8f);
        y += 22;

        Color trickColor = !_player.IsGrounded ? keyColor : new Color(80, 80, 80);
        Color trickDescColor = !_player.IsGrounded ? descColor : new Color(80, 80, 80);

        DrawText("J", panelX + 15, y, trickColor, 0.85f);
        DrawText("Hand Up", panelX + 40, y, trickDescColor, 0.8f);
        y += 22;

        DrawText("K", panelX + 15, y, trickColor, 0.85f);
        DrawText("Hand Down", panelX + 40, y, trickDescColor, 0.8f);
        y += 22;

        DrawText("L", panelX + 15, y, trickColor, 0.85f);
        DrawText("Between Legs", panelX + 40, y, trickDescColor, 0.8f);
        y += 22;

        DrawText("I", panelX + 15, y, trickColor, 0.85f);
        DrawText("Spin!", panelX + 40, y, trickDescColor, 0.8f);
    }

    private void DrawResultsScreen()
    {
        if (_lastAttemptResult == null)
            return;

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), ColorHelper.FromRgba(0, 0, 0, 180));

        int panelX = SCREEN_WIDTH / 2 - 280;
        int panelY = SCREEN_HEIGHT / 2 - 220;
        int panelW = 560;
        int panelH = 440;

        _spriteBatch.Draw(_pixel, new Rectangle(panelX - 4, panelY - 4, panelW + 8, panelH + 8), 
            _lastAttemptResult.DunkSuccessful ? new Color(255, 200, 50) : new Color(200, 50, 50));
        _spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, panelW, panelH), new Color(30, 30, 50));

        string titleText = _lastAttemptResult.DunkSuccessful ? "SLAM DUNK!" : "ATTEMPT FAILED";
        Color titleColor = _lastAttemptResult.DunkSuccessful ? new Color(255, 200, 50) : new Color(255, 80, 80);
        float titleScale = 1.4f + (float)Math.Sin(_totalGameTime / 200) * 0.1f;
        DrawText(titleText, panelX + panelW / 2 - (int)(100 * titleScale), panelY + 25, titleColor, titleScale);

        if (_lastAttemptResult.DunkSuccessful)
        {
            int y = panelY + 90;
            int labelX = panelX + 40;
            int valueX = panelX + panelW - 120;

            DrawScoreLine("Base Dunk Score", _lastAttemptResult.BaseScore, labelX, valueX, ref y, Color.White);
            DrawScoreLine("Trick Bonus", _lastAttemptResult.TrickBonus, labelX, valueX, ref y, new Color(100, 255, 100), "+");
            DrawScoreLine("Variety Bonus", _lastAttemptResult.VarietyBonus, labelX, valueX, ref y, new Color(100, 255, 255), "+");
            DrawScoreLine("Timing Bonus", _lastAttemptResult.TimingBonus, labelX, valueX, ref y, new Color(255, 255, 100), "+");
            DrawScoreLine("Creativity Bonus", _lastAttemptResult.CreativityBonus, labelX, valueX, ref y, new Color(255, 100, 255), "+");
            
            if (_lastAttemptResult.Penalties > 0)
            {
                DrawScoreLine("Penalties", _lastAttemptResult.Penalties, labelX, valueX, ref y, new Color(255, 100, 100), "-");
            }

            y += 10;
            _spriteBatch.Draw(_pixel, new Rectangle(panelX + 30, y, panelW - 60, 3), new Color(255, 200, 50));
            y += 20;

            DrawText("TOTAL SCORE", labelX, y, new Color(255, 200, 50), 1.2f);
            DrawText($"{_lastAttemptResult.TotalScore}", valueX - 20, y, new Color(255, 200, 50), 1.5f);

            y += 40;
            string rating = GetRating(_lastAttemptResult.TotalScore);
            DrawText($"Rating: {rating}", panelX + panelW / 2 - 80, y, GetRatingColor(rating), 1.3f);

            // Show move sequence
            y += 40;
            string moveSequence = _trickTracker.GetComboString();
            DrawText("Moves:", labelX, y, new Color(150, 200, 255), 0.9f);
            DrawText(moveSequence, labelX + 70, y, new Color(200, 255, 200), 0.9f);
        }
        else
        {
            DrawText("You missed the dunk!", panelX + panelW / 2 - 100, panelY + 150, new Color(200, 200, 200), 1.0f);
            DrawText("Get closer to the hoop", panelX + panelW / 2 - 110, panelY + 190, new Color(150, 150, 150), 0.9f);
            DrawText("before landing!", panelX + panelW / 2 - 80, panelY + 220, new Color(150, 150, 150), 0.9f);
            
            // Show moves even on failure
            string moveSequence = _trickTracker.GetComboString();
            if (moveSequence != "None")
            {
                DrawText("Your moves: " + moveSequence, panelX + panelW / 2 - 120, panelY + 280, new Color(150, 150, 200), 0.8f);
            }
        }

        float promptPulse = (float)(Math.Sin(_totalGameTime / 300) + 1) / 2;
        DrawText("Press SPACE to Try Again", panelX + panelW / 2 - 120, panelY + panelH - 50, 
            ColorHelper.FromRgba(255, 255, 100, (int)(150 + 105 * promptPulse)), 1.1f);
    }

    private void DrawScoreLine(string label, int value, int labelX, int valueX, ref int y, Color color, string prefix = "")
    {
        DrawText(label, labelX, y, new Color(200, 200, 200), 0.95f);
        DrawText($"{prefix}{value}", valueX, y, color, 1.0f);
        y += 35;
    }

    private string GetRating(int score)
    {
        if (score >= 300) return "LEGENDARY!";
        if (score >= 250) return "INSANE!";
        if (score >= 200) return "EXCELLENT!";
        if (score >= 150) return "GREAT!";
        if (score >= 100) return "GOOD";
        return "OK";
    }

    private Color GetRatingColor(string rating)
    {
        return rating switch
        {
            "LEGENDARY!" => new Color(255, 200, 50),
            "INSANE!" => new Color(255, 100, 255),
            "EXCELLENT!" => new Color(100, 255, 100),
            "GREAT!" => new Color(100, 200, 255),
            _ => new Color(200, 200, 200)
        };
    }

    private void DrawText(string text, int x, int y, Color color, float scale = 1.0f)
    {
        if (_font != null)
        {
            _spriteBatch.DrawString(_font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        else
        {
            // Fallback: draw simple pixel-based text
            DrawPixelText(text, x, y, color, scale);
        }
    }

    private void DrawPixelText(string text, int x, int y, Color color, float scale)
    {
        int charWidth = (int)(8 * scale);
        int charHeight = (int)(12 * scale);
        int spacing = (int)(2 * scale);
        int currentX = x;

        foreach (char c in text.ToUpper())
        {
            if (c == ' ')
            {
                currentX += charWidth / 2 + spacing;
                continue;
            }

            DrawPixelChar(c, currentX, y, color, scale);
            currentX += charWidth + spacing;
        }
    }

    private void DrawPixelChar(char c, int x, int y, Color color, float scale)
    {
        int s = Math.Max(1, (int)(2 * scale)); // pixel size
        int w = (int)(6 * scale); // char width
        int h = (int)(10 * scale); // char height

        // Simple bitmap font patterns (5x7 grid scaled)
        switch (c)
        {
            case 'A':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y, s, h), color); // center vertical
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s*2, s, h - s*2), color); // left
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s*2, s, h - s*2), color); // right
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2, w, s), color); // middle
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, w, s), color); // top
                break;
            case 'B':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s/2, s, h/2 - s*2), color);
                break;
            case 'C':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s, s), color);
                break;
            case 'D':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h - s*2), color);
                break;
            case 'E':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                break;
            case 'F':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w - s, s), color);
                break;
            case 'G':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y + h/2, w/2, s), color);
                break;
            case 'H':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w, s), color);
                break;
            case 'I':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                break;
            case 'J':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2, s, h/2 - s), color);
                break;
            case 'K':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w/2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y, s, h/2 - s/2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y + h/2 + s/2, s, h/2 - s/2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h*2/3, s, h/3), color);
                break;
            case 'L':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                break;
            case 'M':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + s, s, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s*2, y + s, s, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + s*2, s, s*2), color);
                break;
            case 'N':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + s, s, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/3, s, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s*2, y + h - s*4, s, s*2), color);
                break;
            case 'O':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case 'P':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/2 - s*2), color);
                break;
            case 'Q':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h - s*3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y + h - s*3, w/2, s*3), color);
                break;
            case 'R':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y + h/2 + s/2, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h - s*3, s, s*3), color);
                break;
            case 'S':
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s/2, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w - s, s), color);
                break;
            case 'T':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y, s, h), color);
                break;
            case 'U':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case 'V':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h*2/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h*2/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h*2/3, s, h/3 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s*2, y + h*2/3, s, h/3 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h - s, s, s), color);
                break;
            case 'W':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/2, s, h/2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case 'X':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s, y + h/2 - s, s*2, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 + s, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s, s, h/2 - s), color);
                break;
            case 'Y':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h/2 - s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/2 - s, s, h/2 + s), color);
                break;
            case 'Z':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/3, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h*2/3 - s, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                break;
            case '0':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case '1':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y, s, h), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/4, y + s, s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                break;
            case '2':
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w, s), color);
                break;
            case '3':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s/2, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w - s, s), color);
                break;
            case '4':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s, h/2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h), color);
                break;
            case '5':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w - s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s/2, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h - s, w - s, s), color);
                break;
            case '6':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s/2, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case '7':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h), color);
                break;
            case '8':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 + s/2, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + h/2 + s/2, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case '9':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + s, s, h/2 - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h/2 - s/2, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y + h - s, w - s*2, s), color);
                break;
            case ':':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/4, s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h*3/4 - s, s, s), color);
                break;
            case '!':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y, s, h - s*3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h - s, s, s), color);
                break;
            case '?':
                _spriteBatch.Draw(_pixel, new Rectangle(x + s, y, w - s*2, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y + s, s, h/4), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/2 - s, s, h/4), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h - s, s, s), color);
                break;
            case '.':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h - s, s, s), color);
                break;
            case ',':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h - s*2, s, s*2), color);
                break;
            case '-':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w, s), color);
                break;
            case '+':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/2 - s/2, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/4, s, h/2), color);
                break;
            case '=':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/3, w, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h*2/3 - s, w, s), color);
                break;
            case '(':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y, s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/3, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2, y + h - s, s, s), color);
                break;
            case ')':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s, y, s, s), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w*2/3 - s, y + s, s, h - s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s, y + h - s, s, s), color);
                break;
            case '%':
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, s*2, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s*2, y + h - s*2, s*2, s*2), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/3, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h*2/3, s, h/3), color);
                break;
            case '/':
                _spriteBatch.Draw(_pixel, new Rectangle(x + w - s, y, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x + w/2 - s/2, y + h/3, s, h/3), color);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h*2/3, s, h/3), color);
                break;
            default:
                // Unknown char: draw a small box
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + h/4, w - s, h/2), ColorHelper.FromRgba(color.R, color.G, color.B, 100));
                break;
        }
    }
}
