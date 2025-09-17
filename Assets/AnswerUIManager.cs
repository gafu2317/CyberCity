using UnityEngine;

public class AnswerUIManager : MonoBehaviour
{
    [SerializeField] private GameObject correctUIPrefab; // Prefabをセット
    [SerializeField] private Transform canvasTransform;  // CanvasのTransform
    public void ShowAnswerUI(bool answer)//正解か不正解かのUIをだす。answerがTrueの時に正解
    {
        GameObject ui = Instantiate(correctUIPrefab, canvasTransform);
        ui.transform.localScale = Vector3.zero; // 最初は小さく
        if (answer)
        {
            ui.GetComponent<AnswerUI>().ShowCorrect(); // 正解アニメーション開始
        }
        else
        {
            ui.GetComponent<AnswerUI>().ShowIncorrect();//不正解
        }
    }
}
