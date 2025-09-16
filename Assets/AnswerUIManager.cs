using UnityEngine;

public class AnswerUIManager : MonoBehaviour
{
    [SerializeField] private GameObject correctUIPrefab; // Prefabをセット
    [SerializeField] private Transform canvasTransform;  // CanvasのTransform
    public void ShowCorrectUI()
    {
        GameObject ui = Instantiate(correctUIPrefab, canvasTransform);
        ui.transform.localScale = Vector3.zero; // 最初は小さく
        ui.GetComponent<AnswerUI>().ShowCorrect(); // アニメーション開始
    }
}
