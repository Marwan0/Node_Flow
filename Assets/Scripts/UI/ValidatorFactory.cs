namespace QuizSystem
{
    public static class ValidatorFactory
    {
        public static IQuestionValidator CreateValidator(QuestionData questionData)
        {
            if (questionData == null) return null;

            switch (questionData.questionType)
            {
                case QuestionType.TrueFalse:
                    return new TrueFalseValidator(questionData);
                case QuestionType.FillInTheBlank:
                    return new FillInTheBlankValidator(questionData);
                case QuestionType.MultiSelect:
                    return new MultiSelectValidator(questionData);
                case QuestionType.Ordering:
                    return new OrderingValidator(questionData);
                case QuestionType.Hotspot:
                    return new HotspotValidator(questionData);
                case QuestionType.Slider:
                    return new SliderValidator(questionData);
                case QuestionType.Audio:
                    return new AudioValidator(questionData);
                case QuestionType.MultipleChoice:
                    return new MultipleChoiceValidator(questionData);
                case QuestionType.DragDrop:
                    return new DragDropValidator(questionData);
                case QuestionType.Connect:
                    return new ConnectValidator(questionData);
                default:
                    UnityEngine.Debug.LogWarning($"No validator found for question type: {questionData.questionType}");
                    return null;
            }
        }
    }
}

