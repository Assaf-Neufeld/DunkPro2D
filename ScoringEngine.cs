using System.Collections.Generic;
using System.Linq;

namespace DunkPro2D;

/// <summary>
/// Scoring engine: calculates attempt score based on tricks, timing, variety, and creativity.
/// </summary>
public class ScoringEngine
{
    // Scoring constants
    public const int BASE_DUNK_SCORE = 100;
    public const int POINTS_PER_TRICK = 15;

    public AttemptResult CalculateScore(AttemptData attempt)
    {
        var result = new AttemptResult { DunkSuccessful = attempt.DunkSuccessful };

        if (!attempt.DunkSuccessful)
        {
            result.BaseScore = 0;
            result.TotalScore = 0;
            return result;
        }

        result.BaseScore = BASE_DUNK_SCORE;

        // Trick bonus: points per non-dunk move
        var nonDunkMoves = attempt.Moves.Where(m => m.Type != MoveType.Dunk).ToList();
        result.TrickBonus = nonDunkMoves.Count * POINTS_PER_TRICK;

        // Variety bonus: unique move types
        var uniqueMoves = nonDunkMoves.Select(m => m.Type).Distinct().Count();
        result.VarietyBonus = uniqueMoves * 20;

        // Timing bonus: reward doing moves near jump apex and clean dunk timing
        result.TimingBonus = CalculateTimingBonus(attempt);

        // Creativity bonus: sequence variety and spin
        result.CreativityBonus = CalculateCreativityBonus(nonDunkMoves, attempt.HasSpin);

        // Penalties: repeated moves, late dunk
        result.Penalties = CalculatePenalties(nonDunkMoves);

        result.TotalScore = result.BaseScore + result.TrickBonus + result.VarietyBonus
                          + result.TimingBonus + result.CreativityBonus - result.Penalties;

        return result;
    }

    private int CalculateTimingBonus(AttemptData attempt)
    {
        int bonus = 0;

        // Check if dunk timing is within a good window (e.g., 200-800ms into jump)
        if (attempt.DunkTiming >= 200 && attempt.DunkTiming <= 800)
        {
            bonus += 25; // Good timing window
        }

        // Bonus for doing tricks during mid-jump (not immediately after jump start)
        var mid = attempt.Moves.Where(m => m.Type != MoveType.Dunk && m.TimestampMs > 100 && m.TimestampMs < 600).Count();
        bonus += mid * 5;

        return bonus;
    }

    private int CalculateCreativityBonus(List<MoveEvent> nonDunkMoves, bool hasSpin)
    {
        int bonus = 0;

        // At least 3 moves before dunk
        if (nonDunkMoves.Count >= 3)
            bonus += 30;

        // Bonus for spin
        if (hasSpin)
            bonus += 25;

        // Bonus for good alternation (not all same type)
        if (HasGoodAlternation(nonDunkMoves))
            bonus += 20;

        return bonus;
    }

    private int CalculatePenalties(List<MoveEvent> nonDunkMoves)
    {
        int penalty = 0;

        // Penalty for repeating same move consecutively
        for (int i = 1; i < nonDunkMoves.Count; i++)
        {
            if (nonDunkMoves[i].Type == nonDunkMoves[i - 1].Type)
            {
                penalty += 10;
            }
        }

        return penalty;
    }

    private bool HasGoodAlternation(List<MoveEvent> moves)
    {
        if (moves.Count < 2)
            return false;

        // Check if at least 50% of consecutive pairs are different
        int changes = 0;
        for (int i = 1; i < moves.Count; i++)
        {
            if (moves[i].Type != moves[i - 1].Type)
                changes++;
        }

        return changes >= moves.Count / 2;
    }
}
