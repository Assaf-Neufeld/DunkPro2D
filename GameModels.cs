using System.Collections.Generic;

namespace DunkPro2D;

/// <summary>
/// Enumerations and data structures for game logic.
/// </summary>
public enum GameState
{
    Ready,
    InAttempt,
    Results
}

public enum MoveType
{
    None,
    HandUp,
    HandDown,
    BetweenLegs,
    Spin,
    Dunk
}

public class MoveEvent
{
    public MoveType Type { get; set; }
    public double TimestampMs { get; set; }
}

public class AttemptData
{
    public List<MoveEvent> Moves { get; set; } = new();
    public bool DunkSuccessful { get; set; }
    public int MoveSequenceCount { get; set; }
    public bool HasSpin { get; set; }
    public double TotalAirTime { get; set; }
    public double DunkTiming { get; set; }
}

public class AttemptResult
{
    public int BaseScore { get; set; }
    public int TrickBonus { get; set; }
    public int VarietyBonus { get; set; }
    public int TimingBonus { get; set; }
    public int CreativityBonus { get; set; }
    public int Penalties { get; set; }
    public int TotalScore { get; set; }
    public bool DunkSuccessful { get; set; }
}
