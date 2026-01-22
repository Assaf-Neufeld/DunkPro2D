using DunkPro2D;
using Xunit;

namespace DunkPro2D.Tests;

public class TrickTrackerTests
{
    [Fact]
    public void StartAttempt_SetsInAttemptTrue()
    {
        var tracker = new TrickTracker();
        
        tracker.StartAttempt(0);
        
        Assert.True(tracker.IsInAttempt);
    }

    [Fact]
    public void RecordMove_WhenNotInAttempt_ReturnsFalse()
    {
        var tracker = new TrickTracker();
        
        bool result = tracker.RecordMove(MoveType.HandUp, 100);
        
        Assert.False(result);
    }

    [Fact]
    public void RecordMove_ValidMove_ReturnsTrue()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        bool result = tracker.RecordMove(MoveType.HandUp, 100);
        
        Assert.True(result);
    }

    [Fact]
    public void RecordMove_DuringCooldown_ReturnsFalse()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        tracker.RecordMove(MoveType.HandUp, 100);
        bool result = tracker.RecordMove(MoveType.HandUp, 150); // Only 50ms later, cooldown is 200ms
        
        Assert.False(result);
    }

    [Fact]
    public void RecordMove_AfterCooldown_ReturnsTrue()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        tracker.RecordMove(MoveType.HandUp, 100);
        bool result = tracker.RecordMove(MoveType.HandUp, 350); // 250ms later, cooldown is 200ms
        
        Assert.True(result);
    }

    [Fact]
    public void RecordMove_SpamSameMove_BlocksAfterThreeRepeats()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        // First three should work (with appropriate cooldown gaps)
        Assert.True(tracker.RecordMove(MoveType.HandUp, 0));
        Assert.True(tracker.RecordMove(MoveType.HandUp, 250));
        Assert.True(tracker.RecordMove(MoveType.HandUp, 500));
        
        // Fourth consecutive should be blocked
        Assert.False(tracker.RecordMove(MoveType.HandUp, 750));
    }

    [Fact]
    public void RecordMove_DifferentMoves_AllSucceed()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        Assert.True(tracker.RecordMove(MoveType.HandUp, 0));
        Assert.True(tracker.RecordMove(MoveType.HandDown, 100));
        Assert.True(tracker.RecordMove(MoveType.BetweenLegs, 200));
        Assert.True(tracker.RecordMove(MoveType.Spin, 300));
    }

    [Fact]
    public void RecordMove_Dunk_EndsAttempt()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        tracker.RecordMove(MoveType.HandUp, 100);
        tracker.RecordMove(MoveType.Dunk, 500);
        
        Assert.False(tracker.IsInAttempt);
    }

    [Fact]
    public void GetRecordedMoves_ReturnsAllRecordedMoves()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        tracker.RecordMove(MoveType.HandUp, 100);
        tracker.RecordMove(MoveType.Spin, 300);
        tracker.RecordMove(MoveType.Dunk, 500);
        
        var moves = tracker.GetRecordedMoves();
        
        Assert.Equal(3, moves.Count);
        Assert.Equal(MoveType.HandUp, moves[0].Type);
        Assert.Equal(MoveType.Spin, moves[1].Type);
        Assert.Equal(MoveType.Dunk, moves[2].Type);
    }

    [Fact]
    public void GetComboString_NoMoves_ReturnsNone()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        
        Assert.Equal("None", tracker.GetComboString());
    }

    [Fact]
    public void GetComboString_WithMoves_ReturnsCommaDelimitedString()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        tracker.RecordMove(MoveType.HandUp, 100);
        tracker.RecordMove(MoveType.Spin, 300);
        
        var combo = tracker.GetComboString();
        
        Assert.Contains("Up", combo);
        Assert.Contains("Spin", combo);
    }

    [Fact]
    public void GetCurrentTrick_ReturnsLastRecordedMove()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        tracker.RecordMove(MoveType.HandUp, 100);
        Assert.Equal(MoveType.HandUp, tracker.GetCurrentTrick());
        
        tracker.RecordMove(MoveType.Spin, 300);
        Assert.Equal(MoveType.Spin, tracker.GetCurrentTrick());
    }

    [Fact]
    public void GetStyleMeter_IncreasesWithTricks()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(0);
        
        float initialStyle = tracker.GetStyleMeter();
        tracker.RecordMove(MoveType.HandUp, 100);
        float afterTrick = tracker.GetStyleMeter();
        
        Assert.True(afterTrick > initialStyle);
    }

    [Fact]
    public void GetDunkTiming_ReturnsDunkTimestamp()
    {
        var tracker = new TrickTracker();
        tracker.StartAttempt(0);
        tracker.ResetAttempt(100);
        
        tracker.RecordMove(MoveType.HandUp, 200);
        tracker.RecordMove(MoveType.Dunk, 600);
        
        // Dunk timing should be relative to jump start (600 - 100 = 500)
        Assert.Equal(500, tracker.GetDunkTiming());
    }
}
