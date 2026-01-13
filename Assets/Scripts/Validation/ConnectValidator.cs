using System.Collections.Generic;

namespace QuizSystem
{
    public class ConnectValidator : QuestionValidator
    {
        private ConnectQuestionData connectData;

        public ConnectValidator(QuestionData data) : base(data)
        {
            connectData = data as ConnectQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is Dictionary<int, int> userConnections)
            {
                // Check if all correct connections are present and correct
                bool allCorrect = true;
                int correctCount = 0;
                int totalConnections = connectData.correctConnections.Count;

                foreach (var correctConnection in connectData.correctConnections)
                {
                    if (userConnections.ContainsKey(correctConnection.Key))
                    {
                        if (userConnections[correctConnection.Key] == correctConnection.Value)
                        {
                            correctCount++;
                        }
                        else
                        {
                            allCorrect = false;
                        }
                    }
                    else
                    {
                        allCorrect = false;
                    }
                }

                // Check for extra incorrect connections
                foreach (var userConnection in userConnections)
                {
                    if (!connectData.correctConnections.ContainsKey(userConnection.Key) ||
                        connectData.correctConnections[userConnection.Key] != userConnection.Value)
                    {
                        allCorrect = false;
                    }
                }

                if (allCorrect && userConnections.Count == totalConnections)
                {
                    return new ValidationResult(true, "All connections are correct!");
                }
                else
                {
                    return HandleWrongAnswer();
                }
            }

            return new ValidationResult(false, "Invalid answer format. Expected Dictionary<int, int>.");
        }
    }
}

