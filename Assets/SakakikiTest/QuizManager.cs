using UnityEngine;
using System.Collections.Generic;
using System;

public class QuizManager : MonoBehaviour
{
    // シングルトン
    public static QuizManager Instance { get; private set; }

    // クイズの難易度
    public enum Difficulty
    {
        Easy,
        Normal
    }
    public Difficulty QuizDifficulty { get; set; } = Difficulty.Easy;

    // 現在のクイズ情報
    public String Qusetion { get; private set; }
    public String[] Choices { get; private set; }
    public int CorrectIndex { get; private set; }

    // クイズデータの格納リスト
    private List<MultiChoicesQuiz>[] QuizLists_Easy = new List<MultiChoicesQuiz>[7];
    private List<MultiChoicesQuiz>[] QuizLists_Normal = new List<MultiChoicesQuiz>[7];

    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // クイズデータの初期化とロード・格納
        for (int i = 0; i < QuizLists_Easy.Length; i++)
        {
            QuizLists_Easy[i] = new List<MultiChoicesQuiz>();
        }
        UnityEngine.Object[] quizArray_Easy = Resources.LoadAll("Quiz/Easy");
        foreach (UnityEngine.Object quizObj in quizArray_Easy)
        {
            MultiChoicesQuiz quiz = (MultiChoicesQuiz)quizObj;
            QuizLists_Easy[quiz.Number - 1].Add(quiz);
        }

        for (int i = 0; i < QuizLists_Normal.Length; i++)
        {
            QuizLists_Normal[i] = new List<MultiChoicesQuiz>();
        }
        UnityEngine.Object[] quizArray_Normal = Resources.LoadAll("Quiz/Normal");
        foreach (UnityEngine.Object quizObj in quizArray_Normal)
        {
            MultiChoicesQuiz quiz = (MultiChoicesQuiz)quizObj;
            QuizLists_Normal[quiz.Number - 1].Add(quiz);
        }
    }

    /// <summary>
    /// クイズの難易度を設定する
    /// </summary>
    /// <param name="difficulty">設定する難易度</param>
    public void SetDifficulty(Difficulty difficulty)
    {
        QuizDifficulty = difficulty;
    }

    /// <summary>
    /// 出題するクイズをランダムに選択し、現在のクイズ情報を更新する
    /// </summary>
    /// <param name="number">出題番号（1～7）</param>
    public void SetRandomQuiz(int number)
    {
        if (number < 1 || number > 7)
        {
            Debug.LogError("出題番号は1から7の範囲で指定してください。");
            return;
        }

        List<MultiChoicesQuiz> selectedList = QuizDifficulty == Difficulty.Easy ? QuizLists_Easy[number - 1] : QuizLists_Normal[number - 1];

        if (selectedList.Count == 0)
        {
            Debug.LogError("指定された出題番号のクイズが存在しません。");
            return;
        }

        // ランダムにクイズを選択
        int randomIndex = UnityEngine.Random.Range(0, selectedList.Count);
        MultiChoicesQuiz selectedQuiz = selectedList[randomIndex];

        // クイズ情報の更新
        Qusetion = selectedQuiz.Qusetion;

        // 選択肢のシャッフル
        Choices = (string[])selectedQuiz.Choices.Clone();
        for (int i = Choices.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            string temp = Choices[i];
            Choices[i] = Choices[j];
            Choices[j] = temp;
        }

        // 正解のインデックスを更新
        CorrectIndex = Array.IndexOf(Choices, selectedQuiz.Choices[0]);
    }
}
