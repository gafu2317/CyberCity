using UnityEngine;

public class UIGenerator : MonoBehaviour
{
    [SerializeField] private GameObject quizeUIPrefab; // Prefabをセット
    [SerializeField] private Transform canvasTransform;  //UIを表示するCanvasを指定
    public UIControlller uIControlller;
    public void ShowQuizUI(int panelNum)
    {
        uIControlller.targetPanelNum = panelNum;//ここでpanelの番号を取得
        Instantiate(quizeUIPrefab, canvasTransform);
        uIControlller.SlideIn();
        
    }
}
