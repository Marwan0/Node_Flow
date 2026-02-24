using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "FillInTheBlankQuestion", menuName = "Quiz System/Fill in the Blank Question")]
    public class FillInTheBlankQuestionData : QuestionData
    {
        [Header("Answer")]
        [Tooltip("The correct answer (case-sensitive if Case Sensitive is enabled)")]
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

        public bool IsAnswerCorrect(string userAnswer)
        {
            if (string.IsNullOrEmpty(userAnswer))
                return false;

            string normalizedUser = caseSensitive ? userAnswer : userAnswer.ToLower();
            string normalizedCorrect = caseSensitive ? correctAnswer : correctAnswer.ToLower();

            // Exact match
            if (normalizedUser == normalizedCorrect)
                return true;

            // Check alternatives
            foreach (var alt in alternativeAnswers)
            {
                string normalizedAlt = caseSensitive ? alt : alt.ToLower();
                if (normalizedUser == normalizedAlt)
                    return true;
            }

            // Partial match if enabled
            if (allowPartialMatch)
            {
                if (normalizedUser.Contains(normalizedCorrect) || normalizedCorrect.Contains(normalizedUser))
                {
                    float similarity = CalculateSimilarity(normalizedUser, normalizedCorrect);
                    return similarity >= partialMatchThreshold;
                }
            }

            return false;
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
