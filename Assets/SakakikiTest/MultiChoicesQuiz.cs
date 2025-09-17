using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="選択式クイズ")]
public class MultiChoicesQuiz : ScriptableObject
{
    [Header("問題文")]
    [TextArea(1, 10)]
    public string Qusetion;

    [Header("出題番号(1～7)")]
    public int Number;

    [Header("選択肢\n要素0：正解\n要素1〜3：不正解（1から優先的に使用）")]
    public string[] Choices = new string[4];
}