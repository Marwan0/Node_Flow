using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [System.Serializable]
    public class FillBlankSlot
    {
        [Tooltip("Correct text for this blank (case sensitivity follows question settings)")]
        public string correctAnswer = "";

        [Tooltip("Other acceptable answers for this blank")]
        public List<string> alternativeAnswers = new List<string>();
    }

    [CreateAssetMenu(fileName = "FillInTheBlankQuestion", menuName = "Quiz System/Fill in the Blank Question")]
    public class FillInTheBlankQuestionData : QuestionData
    {
        [Header("Blanks (ordered)")]
        [Tooltip("One entry per blank, top to bottom in the UI. Leave empty to use the single Correct Answer fields below.")]
        public List<FillBlankSlot> blanks = new List<FillBlankSlot>();

        [Header("Single blank (legacy)")]
        [Tooltip("Used only when the Blanks list is empty.")]
        public string correctAnswer = "";

        [Tooltip("Alternative acceptable answers (synonyms, variations)")]
        public List<string> alternativeAnswers = new List<string>();

        [Tooltip("Whether the answer is case-sensitive")]
        public bool caseSensitive = false;

        [Tooltip("Allow partial matches (useful for longer answers)")]
        public bool allowPartialMatch = false;

        [Tooltip("Minimum similarity required for partial match (0.5 = 50% match)")]
        [Range(0.5f, 1.0f)]
        public float partialMatchThreshold = 0.8f;

        private void OnEnable()
        {
            questionType = QuestionType.FillInTheBlank;
        }

        public int GetBlankSlotCount()
        {
            if (blanks != null && blanks.Count > 0)
                return blanks.Count;
            return 1;
        }

        private void ResolveSlot(int index, out string primary, out List<string> alts)
        {
            if (blanks != null && blanks.Count > 0)
            {
                var slot = blanks[index];
                primary = slot.correctAnswer ?? "";
                alts = slot.alternativeAnswers ?? new List<string>();
                return;
            }

            if (index == 0)
            {
                primary = correctAnswer ?? "";
                alts = alternativeAnswers ?? new List<string>();
                return;
            }

            primary = "";
            alts = new List<string>();
        }

        public bool IsSlotAnswerCorrect(int slotIndex, string userAnswer)
        {
            if (string.IsNullOrEmpty(userAnswer))
                return false;

            ResolveSlot(slotIndex, out string correctRaw, out List<string> alts);
            if (string.IsNullOrEmpty(correctRaw) && (alts == null || alts.Count == 0))
                return false;

            string normalizedUser = caseSensitive ? userAnswer : userAnswer.ToLower();

            if (!string.IsNullOrEmpty(correctRaw))
            {
                string normCorrect = caseSensitive ? correctRaw : correctRaw.ToLower();
                if (normalizedUser == normCorrect)
                    return true;
            }

            if (alts != null)
            {
                foreach (var alt in alts)
                {
                    if (string.IsNullOrEmpty(alt)) continue;
                    string normalizedAlt = caseSensitive ? alt : alt.ToLower();
                    if (normalizedUser == normalizedAlt)
                        return true;
                }
            }

            if (allowPartialMatch && !string.IsNullOrEmpty(correctRaw))
            {
                string nc = caseSensitive ? correctRaw : correctRaw.ToLower();
                if (normalizedUser.Contains(nc) || nc.Contains(normalizedUser))
                {
                    float similarity = CalculateSimilarity(normalizedUser, nc);
                    return similarity >= partialMatchThreshold;
                }
            }

            return false;
        }

        /// <summary>Single-blank validation (legacy API).</summary>
        public bool IsAnswerCorrect(string userAnswer)
        {
            if (GetBlankSlotCount() != 1)
                return false;
            return IsSlotAnswerCorrect(0, userAnswer);
        }

        public bool AreAllAnswersCorrect(IReadOnlyList<string> userAnswers)
        {
            if (userAnswers == null)
                return false;

            int n = GetBlankSlotCount();
            if (userAnswers.Count != n)
                return false;

            for (int i = 0; i < n; i++)
            {
                if (!IsSlotAnswerCorrect(i, userAnswers[i]))
                    return false;
            }

            return true;
        }

        public string GetJoinedCorrectAnswersDisplay(string separator = " | ")
        {
            int n = GetBlankSlotCount();
            if (n == 0) return "";

            var parts = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                parts.Add(GetPrimaryCorrectAnswerForDisplay(i));
            }

            return string.Join(separator, parts);
        }

        /// <summary>
        /// Canonical text shown for a blank (primary answer, else first alternative, else "?").
        /// </summary>
        public string GetPrimaryCorrectAnswerForDisplay(int slotIndex)
        {
            ResolveSlot(slotIndex, out string primary, out List<string> alts);
            if (!string.IsNullOrEmpty(primary))
                return primary;
            if (alts != null)
            {
                foreach (var a in alts)
                {
                    if (!string.IsNullOrEmpty(a))
                        return a;
                }
            }

            return "?";
        }

        private float CalculateSimilarity(string str1, string str2)
        {
            int maxLen = Mathf.Max(str1.Length, str2.Length);
            if (maxLen == 0) return 1.0f;

            int matches = 0;
            int minLen = Mathf.Min(str1.Length, str2.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (str1[i] == str2[i]) matches++;
            }

            return (float)matches / maxLen;
        }
    }
}
