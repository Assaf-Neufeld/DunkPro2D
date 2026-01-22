using System;
using System.Collections.Generic;
using System.Linq;

namespace DunkPro2D;

/// <summary>
/// Tracks trick moves, enforces cooldowns, and manages style meter.
/// </summary>
public class TrickTracker
{
    // Cooldown per move type (ms)
    private const float MOVE_COOLDOWN = 200f;

    // Style meter: decays when not doing tricks, increases with good combos
    private float _styleMeter = 0f;
    private const float STYLE_DECAY_PER_SEC = 15f;
    private const float STYLE_GAIN_PER_TRICK = 8f;

    // Attempt tracking
    private List<MoveEvent> _recordedMoves = new();
    private Dictionary<MoveType, double> _lastMoveTime = new();
    private bool _inAttempt = false;
    private double _attemptStartTime = 0;
    private double _jumpStartTime = 0;
    private int _consecutiveRepeats = 0;
    private MoveType _lastMoveType = MoveType.None;
    private MoveType _currentTrick = MoveType.None;
    private double _currentTrickTime = 0;

    public bool IsInAttempt => _inAttempt;

    public void StartAttempt(double gameTime)
    {
        _inAttempt = true;
        _attemptStartTime = gameTime;
        _recordedMoves.Clear();
        _lastMoveTime.Clear();
        _consecutiveRepeats = 0;
        _lastMoveType = MoveType.None;
        _styleMeter = 0f;
        _currentTrick = MoveType.None;
    }

    public void ResetAttempt(double gameTime)
    {
        // Called on jump; reset move timestamps for new jump window
        _jumpStartTime = gameTime;
        _lastMoveTime.Clear();
        _consecutiveRepeats = 0;
        _lastMoveType = MoveType.None;
        _currentTrick = MoveType.None;
    }

    public bool RecordMove(MoveType moveType, double gameTime)
    {
        if (!_inAttempt)
            return false;

        // Check cooldown for this move type
        if (_lastMoveTime.TryGetValue(moveType, out double lastTime))
        {
            if (gameTime - lastTime < MOVE_COOLDOWN)
                return false; // Still in cooldown
        }

        // Only record non-spam moves
        if (moveType == _lastMoveType)
        {
            _consecutiveRepeats++;
            if (_consecutiveRepeats > 2)
                return false; // Prevent spam
        }
        else
        {
            _consecutiveRepeats = 0;
        }

        _lastMoveType = moveType;
        _lastMoveTime[moveType] = gameTime;
        _recordedMoves.Add(new MoveEvent { Type = moveType, TimestampMs = gameTime - _jumpStartTime });

        // Set current trick for animation
        _currentTrick = moveType;
        _currentTrickTime = gameTime;

        // Style gain
        _styleMeter = Math.Min(100f, _styleMeter + STYLE_GAIN_PER_TRICK);

        // If Dunk, end attempt
        if (moveType == MoveType.Dunk)
        {
            _inAttempt = false;
        }

        return true;
    }

    public MoveType GetCurrentTrick()
    {
        return _currentTrick;
    }

    public List<MoveEvent> GetRecordedMoves() => new List<MoveEvent>(_recordedMoves);

    public string GetComboString()
    {
        if (_recordedMoves.Count == 0)
            return "None";

        return string.Join(", ", _recordedMoves.Select(m => MoveTypeToString(m.Type)));
    }

    public float GetStyleMeter()
    {
        // Decay style meter over time
        _styleMeter = Math.Max(0f, _styleMeter - (float)STYLE_DECAY_PER_SEC * 0.016f); // ~60fps
        return _styleMeter;
    }

    public double GetTotalAirTime()
    {
        if (_recordedMoves.Count == 0)
            return 0;
        return _recordedMoves.Last().TimestampMs;
    }

    public double GetDunkTiming()
    {
        var dunk = _recordedMoves.FirstOrDefault(m => m.Type == MoveType.Dunk);
        return dunk?.TimestampMs ?? 0;
    }

    private string MoveTypeToString(MoveType type)
    {
        return type switch
        {
            MoveType.HandUp => "Up",
            MoveType.HandDown => "Down",
            MoveType.BetweenLegs => "Legs",
            MoveType.Spin => "Spin",
            MoveType.Dunk => "Dunk",
            _ => "?"
        };
    }
}
