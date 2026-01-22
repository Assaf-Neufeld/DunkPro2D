using DunkPro2D;
using Xunit;

namespace DunkPro2D.Tests;

public class ScoringEngineTests
{
    private readonly ScoringEngine _engine = new();

    [Fact]
    public void CalculateScore_FailedDunk_ReturnsZero()
    {
        var attempt = new AttemptData { DunkSuccessful = false };

        var result = _engine.CalculateScore(attempt);

        Assert.False(result.DunkSuccessful);
        Assert.Equal(0, result.BaseScore);
        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void CalculateScore_SuccessfulDunkNoTricks_ReturnsBaseScore()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500
        };

        var result = _engine.CalculateScore(attempt);

        Assert.True(result.DunkSuccessful);
        Assert.Equal(ScoringEngine.BASE_DUNK_SCORE, result.BaseScore);
        Assert.True(result.TotalScore >= ScoringEngine.BASE_DUNK_SCORE);
    }

    [Fact]
    public void CalculateScore_WithTricks_AddsTrickBonus()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 100 },
                new MoveEvent { Type = MoveType.HandDown, TimestampMs = 200 },
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500
        };

        var result = _engine.CalculateScore(attempt);

        // 2 tricks * 15 points each = 30 trick bonus
        Assert.Equal(2 * ScoringEngine.POINTS_PER_TRICK, result.TrickBonus);
    }

    [Fact]
    public void CalculateScore_WithVariety_AddsVarietyBonus()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 100 },
                new MoveEvent { Type = MoveType.Spin, TimestampMs = 200 },
                new MoveEvent { Type = MoveType.BetweenLegs, TimestampMs = 300 },
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500
        };

        var result = _engine.CalculateScore(attempt);

        // 3 unique moves * 20 points each = 60 variety bonus
        Assert.Equal(3 * 20, result.VarietyBonus);
    }

    [Fact]
    public void CalculateScore_WithSpin_AddsCreativityBonus()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            HasSpin = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.Spin, TimestampMs = 200 },
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500
        };

        var result = _engine.CalculateScore(attempt);

        // Spin adds 25 to creativity bonus
        Assert.True(result.CreativityBonus >= 25);
    }

    [Fact]
    public void CalculateScore_RepeatedMoves_AddsPenalty()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 100 },
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 200 }, // Repeat
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 300 }, // Repeat again
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500
        };

        var result = _engine.CalculateScore(attempt);

        // 2 consecutive repeats * 10 penalty each = 20 penalty
        Assert.Equal(20, result.Penalties);
    }

    [Fact]
    public void CalculateScore_GoodTiming_AddsTimingBonus()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 300 }, // Mid-jump
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500 // Good timing window (200-800ms)
        };

        var result = _engine.CalculateScore(attempt);

        // Should have timing bonus for good dunk timing and mid-jump trick
        Assert.True(result.TimingBonus > 0);
    }

    [Fact]
    public void CalculateScore_ThreeOrMoreMoves_AddsExtraCreativityBonus()
    {
        var attempt = new AttemptData
        {
            DunkSuccessful = true,
            Moves = new List<MoveEvent>
            {
                new MoveEvent { Type = MoveType.HandUp, TimestampMs = 100 },
                new MoveEvent { Type = MoveType.HandDown, TimestampMs = 200 },
                new MoveEvent { Type = MoveType.BetweenLegs, TimestampMs = 300 },
                new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 }
            },
            DunkTiming = 500
        };

        var result = _engine.CalculateScore(attempt);

        // 3+ tricks before dunk adds 30 to creativity bonus
        Assert.True(result.CreativityBonus >= 30);
    }
}
