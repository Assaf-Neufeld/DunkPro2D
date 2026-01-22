using DunkPro2D;
using Xunit;

namespace DunkPro2D.Tests;

public class GameModelsTests
{
    [Fact]
    public void MoveEvent_DefaultValues_AreCorrect()
    {
        var moveEvent = new MoveEvent();
        
        Assert.Equal(MoveType.None, moveEvent.Type);
        Assert.Equal(0, moveEvent.TimestampMs);
    }

    [Fact]
    public void MoveEvent_CanSetProperties()
    {
        var moveEvent = new MoveEvent
        {
            Type = MoveType.Spin,
            TimestampMs = 500
        };
        
        Assert.Equal(MoveType.Spin, moveEvent.Type);
        Assert.Equal(500, moveEvent.TimestampMs);
    }

    [Fact]
    public void AttemptData_DefaultValues()
    {
        var attempt = new AttemptData();
        
        Assert.False(attempt.DunkSuccessful);
        Assert.False(attempt.HasSpin);
        Assert.Equal(0, attempt.DunkTiming);
        Assert.NotNull(attempt.Moves);
        Assert.Empty(attempt.Moves);
    }

    [Fact]
    public void AttemptData_CanAddMoves()
    {
        var attempt = new AttemptData();
        attempt.Moves.Add(new MoveEvent { Type = MoveType.HandUp, TimestampMs = 100 });
        attempt.Moves.Add(new MoveEvent { Type = MoveType.Dunk, TimestampMs = 500 });
        
        Assert.Equal(2, attempt.Moves.Count);
    }

    [Fact]
    public void AttemptResult_DefaultValues()
    {
        var result = new AttemptResult();
        
        Assert.False(result.DunkSuccessful);
        Assert.Equal(0, result.BaseScore);
        Assert.Equal(0, result.TrickBonus);
        Assert.Equal(0, result.VarietyBonus);
        Assert.Equal(0, result.TimingBonus);
        Assert.Equal(0, result.CreativityBonus);
        Assert.Equal(0, result.Penalties);
        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void AttemptResult_CanSetAllProperties()
    {
        var result = new AttemptResult
        {
            DunkSuccessful = true,
            BaseScore = 100,
            TrickBonus = 30,
            VarietyBonus = 40,
            TimingBonus = 25,
            CreativityBonus = 50,
            Penalties = 10,
            TotalScore = 235
        };
        
        Assert.True(result.DunkSuccessful);
        Assert.Equal(100, result.BaseScore);
        Assert.Equal(30, result.TrickBonus);
        Assert.Equal(40, result.VarietyBonus);
        Assert.Equal(25, result.TimingBonus);
        Assert.Equal(50, result.CreativityBonus);
        Assert.Equal(10, result.Penalties);
        Assert.Equal(235, result.TotalScore);
    }

    [Theory]
    [InlineData(MoveType.None)]
    [InlineData(MoveType.HandUp)]
    [InlineData(MoveType.HandDown)]
    [InlineData(MoveType.BetweenLegs)]
    [InlineData(MoveType.Spin)]
    [InlineData(MoveType.Dunk)]
    public void MoveType_AllValuesExist(MoveType moveType)
    {
        // Verify all expected move types are defined
        Assert.True(Enum.IsDefined(typeof(MoveType), moveType));
    }

    [Theory]
    [InlineData(GameState.Ready)]
    [InlineData(GameState.InAttempt)]
    [InlineData(GameState.Results)]
    public void GameState_AllValuesExist(GameState state)
    {
        // Verify all expected game states are defined
        Assert.True(Enum.IsDefined(typeof(GameState), state));
    }
}
