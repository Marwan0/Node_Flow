using UnityEngine;
using QuizSystem;

public class QuizEndAnimator : MonoBehaviour
{
    private void OnEnable() => QuizState.OnQuizCompleted += OnQuizCompleted;
    private void OnDisable() => QuizState.OnQuizCompleted -= OnQuizCompleted;

    public void OnQuizCompleted()
    {
        string finalPattern = QuizState.Instance.GetAnswerOrderString("R", "W", "", includePartialAnswers: false, includeFinalAnswers: true);
        string partialPattern = QuizState.Instance.GetPartialAnswerOrderString("R", "W", "");
        string combinedPattern = QuizState.Instance.GetAnswerOrderString("R", "W", "", includePartialAnswers: true, includeFinalAnswers: true);

        // Example:
        // finalPattern   -> question-level only (e.g. "RRR")
        // partialPattern -> per-step only (e.g. "RRWRR")
        // combinedPattern-> chronological partial + final (e.g. "RRWRRRR")
        Debug.Log($"Answer pattern: {combinedPattern}");
        Debug.Log($"Final pattern: {finalPattern}");
        Debug.Log($"Partial pattern: {partialPattern}");

        var timeline = QuizState.Instance.AnswerTimeline;
        foreach (var entry in timeline)
            Debug.Log($"Question {entry.questionIndex}: {(entry.wasCorrect ? "Correct" : "Wrong")} (Score after: {entry.scoreAfterAnswer})");
        // timeline[i].wasCorrect, timeline[i].scoreAfterAnswer, etc.

        var scoreTimeline = QuizState.Instance.ScoreTimeline;
        foreach (var score in scoreTimeline)
        {
            string stage = score.stage == QuizState.ScoreRecordStage.Partial ? "Partial" : "Final";
            Debug.Log(
                $"[{stage}] Q{score.questionIndex} " +
                $"raw {score.rawTargetAfterEvent}/{score.questionRawMax} " +
                $"(+raw {score.rawDeltaThisEvent}, +score {score.distributedDeltaThisEvent}) " +
                $"norm {score.normalizedProgress:0.00} " +
                $"totalScore {score.scoreAfterEvent}");
        }

        var partialOnly = QuizState.Instance.GetScoreTimelineArray(includePartial: true, includeFinal: false);
        var finalOnly = QuizState.Instance.GetScoreTimelineArray(includePartial: false, includeFinal: true);
        Debug.Log($"Score records -> partial: {partialOnly.Length}, final: {finalOnly.Length}");
    }
}
