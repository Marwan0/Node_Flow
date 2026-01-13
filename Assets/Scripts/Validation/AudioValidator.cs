namespace QuizSystem
{
    public class AudioValidator : QuestionValidator
    {
        private AudioQuestionData audioData;

        public AudioValidator(QuestionData data) : base(data)
        {
            audioData = data as AudioQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (audioData.answerType == AudioAnswerType.MultipleChoice)
            {
                if (answer is int selectedIndex)
                {
                    if (selectedIndex == audioData.correctAnswerIndex)
                    {
                        return new ValidationResult(true, "Correct!");
                    }
                    else
                    {
                        return HandleWrongAnswer();
                    }
                }
            }
            else if (audioData.answerType == AudioAnswerType.FillInTheBlank)
            {
                if (answer is string userAnswer)
                {
                    string normalizedUser = audioData.caseSensitive ? userAnswer : userAnswer.ToLower();
                    string normalizedCorrect = audioData.caseSensitive ? audioData.correctAnswerText : audioData.correctAnswerText.ToLower();

                    if (normalizedUser == normalizedCorrect)
                    {
                        return new ValidationResult(true, "Correct!");
                    }
                    else
                    {
                        return HandleWrongAnswer();
                    }
                }
            }

            return new ValidationResult(false, "Invalid answer format.");
        }
    }
}

