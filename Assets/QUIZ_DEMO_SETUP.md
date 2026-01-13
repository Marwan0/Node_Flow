# Quiz System Demo Setup Guide

This guide will help you set up a working demo of the quiz system.

## Step 1: Create Sample Questions

### Using the Editor Window (Recommended)

1. Open Unity Editor
2. Go to **Tools > Quiz System > Question Editor**
3. Click **"Load All Questions"** button
4. For each question type, click the **"Create [Type] Question"** button
5. Configure your questions in the inspector

### Quick Setup: Create a Multiple Choice Question

1. In the Question Editor window, go to the **"Multiple Choice"** tab
2. Click **"Create Multiple Choice Question"**
3. In the inspector, set:
   - **Question Text**: "What is the capital of France?"
   - **Answers**: 
     - [0] "Paris"
     - [1] "London"
     - [2] "Berlin"
     - [3] "Madrid"
   - **Correct Answer Index**: 0
   - **Hints**: 
     - [0] "It's a famous city known for the Eiffel Tower"
     - [1] "It starts with the letter P"
     - [2] "It's in the north of France"
   - **Max Attempts**: 3
   - **Points**: 10

## Step 2: Create UI Prefabs

You need to create prefabs for each question type. Here's how to create a basic Multiple Choice UI prefab:

### Multiple Choice UI Prefab

1. Create a new GameObject in the scene (right-click in Hierarchy > UI > Canvas if needed)
2. Name it "MultipleChoiceUI"
3. Add the `MultipleChoiceUI` component
4. Set up the UI structure:
   ```
   MultipleChoiceUI (GameObject with MultipleChoiceUI component)
   ├── QuestionText (TextMeshProUGUI)
   ├── HintPanel (GameObject)
   │   └── HintText (TextMeshProUGUI)
   ├── AttemptCounter (TextMeshProUGUI)
   ├── AnswerButton1 (Button)
   │   └── AnswerText1 (TextMeshProUGUI)
   ├── AnswerButton2 (Button)
   │   └── AnswerText2 (TextMeshProUGUI)
   ├── AnswerButton3 (Button)
   │   └── AnswerText3 (TextMeshProUGUI)
   └── AnswerButton4 (Button)
       └── AnswerText4 (TextMeshProUGUI)
   ```
5. Assign references in the MultipleChoiceUI component:
   - `questionText` → QuestionText
   - `hintText` → HintText
   - `hintPanel` → HintPanel
   - `attemptCounterText` → AttemptCounter
   - `answerButtons[0-3]` → AnswerButton1-4
   - `answerTexts[0-3]` → AnswerText1-4
6. Drag the GameObject to `Assets/Prefabs/` folder to create a prefab
7. Delete the instance from the scene (keep the prefab)

### Repeat for Other Question Types

Create similar prefabs for:
- TrueFalseUI
- FillInTheBlankUI
- DragDropUI
- ConnectUI
- etc.

## Step 3: Set Up the Demo Scene

1. Open `Scenes/SampleScene.unity` (or create a new scene)
2. Create a Canvas if one doesn't exist:
   - Right-click in Hierarchy > UI > Canvas
3. Create an empty GameObject and name it "QuizManager"
4. Add the `QuizManager` component to it
5. Create a child GameObject under Canvas called "QuestionContainer" (this will hold question UI instances)
6. In the QuizManager inspector:
   - **Question Container**: Drag the QuestionContainer GameObject
   - **Multiple Choice UI Prefab**: Drag your MultipleChoiceUI prefab
   - **True/False UI Prefab**: Drag your TrueFalseUI prefab
   - (Assign other prefabs as needed)
   - **Questions**: Click the "+" button and add your question ScriptableObjects
7. Add a button to start the quiz (optional):
   - Create UI > Button
   - Name it "StartQuizButton"
   - In the OnClick event, add QuizManager and select `StartQuiz()` method

## Step 4: Test the Demo

1. Press Play in Unity
2. Click the "Start Quiz" button (or use the QuizManager's "Start Quiz" button in inspector)
3. Answer questions and see the hint system in action
4. Try wrong answers to see hints appear
5. After max attempts, see auto-correct

## Quick Demo Script

You can also use the `QuizDemoHelper.cs` script (see below) to quickly create sample questions programmatically.

## Troubleshooting

- **"No UI prefab found"**: Make sure you've assigned all required UI prefabs in QuizManager
- **"Question is null"**: Make sure you've added questions to the QuizManager's questions list
- **UI not showing**: Check that QuestionContainer is assigned and the prefab has the correct component
- **Buttons not working**: Ensure EventSystem exists in the scene (Unity usually creates this automatically with Canvas)

