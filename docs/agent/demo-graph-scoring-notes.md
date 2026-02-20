# Demo Graph Scoring Notes

Scope
- Graph under test: `Assets/NodeGraph/Demo.asset`.
- Keep this file as the working reference for score/progress behavior in that graph.

Current graph facts
- `StartQuizNode`: `totalQuestions = 2`, `maxScore = 100`.
- Score progress uses `ScoreProgressBarNode` with:
  - `valueSource = QuizScore`
  - `useQuizRange = true`
  - slider target path: `QuestionContainerCanvas/Slider`
- Flow order:
  - Start -> StartQuiz -> ScoreProgressBar -> Connect `LoadQuestionNode` -> MultipleChoice `LoadQuestionNode`.

Scoring expectations (for this graph)
- If both question assets are `points = 50`:
  - Full Connect + full MultipleChoice => `100/100` => slider `1.0`.
  - Connect `2/3` correct + full MultipleChoice:
    - Connect contributes about `33` (`50 * 2/3`, rounded),
    - MultipleChoice contributes `50`,
    - Total about `83/100` => slider about `0.83` (roughly `0.85` target expectation).

Important data mismatch found
- `Assets/Data/Questions/Sample_Connect_Question.asset` has `points: 50`.
- `Assets/Data/Questions/Sample_MultipleChoice_Question.asset` has `points: 50`.
- `Assets/Data/Questions/Sample_MultipleChoice_Question 1.asset` has `points: 90`.
- If the graph references the `90`-point question, reaching slider `1.0` can happen earlier than expected.

Debug checklist for this exact graph
1. Confirm which MultipleChoice asset is linked by `LoadQuestionNode` in `Demo.asset`.
2. Confirm both question `points` match intended distribution.
3. Confirm no extra `ScoreNode`/`TimeBonusNode` path modifies score during this sequence.
4. Watch runtime logs for score config mismatch warnings from `QuizManager`.

Design rule to preserve
- Progress should represent: `QuizState.currentScore / QuizState.maxPossibleScore`.
- Question contribution should come from authored question points and per-question progress (for Connect).
