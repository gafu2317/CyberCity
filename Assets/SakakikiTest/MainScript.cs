using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainScript : MonoBehaviour
{
    // ゲーム状態保持
    [SerializeField] private int _quizNum = 0;
    [SerializeField] private int _finalQuizNum = 7;
    [SerializeField] private int _missCount = 0;
    [SerializeField] private int _missLimit = 2;
    private bool _isCleared = false;

    // 入力保持
    private int _playerAnswerNum;
    private float _resetHoldTime = 0;

    private void Start()
    {
        StartCoroutine(MainCoroutine());  
    }

    private void Update()
    {
        // リセット判定
        if (Input.GetKey(KeyCode.R))
        {
            _resetHoldTime += Time.deltaTime;
        }
        else
        {
            _resetHoldTime = 0;
        }
        if (_resetHoldTime > 3)
        {
            // シーンを再ロードすることによるリセット
            ResetCurrentScene();
        }
    }

    IEnumerator MainCoroutine()
    {
        /* -- ゲーム開始前待機画面 -- */

        // ゲーム開始の入力があるまで待機
        while (!Input.GetKeyDown(KeyCode.S))
        {
            /* 処理：謎解き結果の反映など、ゲームの初期設定の受付 */

            // 難易度変更
            // このスクリプト内で難易度は_finalQuizNumの値で保持
            if (Input.GetKeyDown(KeyCode.E))
            {
                _finalQuizNum = 5;
                QuizManager.Instance.SetDifficulty(QuizManager.Difficulty.Easy);
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                _finalQuizNum = 7;
                QuizManager.Instance.SetDifficulty(QuizManager.Difficulty.Normal);
            }

            yield return null;
        }


        /* -- ゲーム開始時アニメーション -- */

        /* 処理：アニメーション開始 */

        /* 処理：カメラ移動処理開始 */

        /* 待機：上記2つの実行完了 */


        /* -- クイズ出題ループ -- */
        while (_quizNum < _finalQuizNum)
        {
            // 次の問題番号に移行
            _quizNum++;

            /* 処理：クイズ出題アニメーション開始 */
            /* 待機：アニメーション完了 */

            /* 処理：カウントダウンアニメーションを開始 */

            // カウントダウンアニメーションを再生しながら入力を待機
            // カウントダウンが終わっても回答を締め切ったりはしない
            _playerAnswerNum = 0;
            while (_playerAnswerNum == 0)
            {
                // プレイヤーの回答を格納
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    _playerAnswerNum = 1;
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    _playerAnswerNum = 2;
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    _playerAnswerNum = 3;
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    _playerAnswerNum = 4;

                yield return null;
            }

            /* 処理：カウントダウンアニメーションの強制終了 */

            // 正誤判定
            if (_playerAnswerNum == QuizManager.Instance.CorrectIndex + 1)
            {
                /* 処理：正解アニメーションを開始（問題番号によって内容は変化） */
                /* 待機：アニメーション完了 */

                // 最終問題を正解したらゲームクリア
                if (_quizNum == _finalQuizNum) _isCleared = true;
            }
            else
            {
                _missCount++;

                /* 処理：不正解アニメーションを開始（問題番号によって内容は変化） */
                /* 待機：アニメーション完了 */

                // 不正解の回数が規定値を超えたらゲームオーバー
                if (_missCount > _missLimit) break;
            }
        }


        /* -- リザルト演出 -- */

        // _quizNum の値からゲームクリアかどうかを判定
        if (_isCleared)
        {
            /* 処理：ゲームクリアアニメーションを開始 */
        }
        else
        {
            /* 処理：ゲームオーバーアニメーションを開始 */
        }


        // ゲームクリア後のリセット処理はゲーム全体共通のリセット入力に委託
    }

    /// <summary>
    /// 現在のシーンを再ロードするメソッド
    /// </summary>
    public void ResetCurrentScene()
    {
        // 現在アクティブなシーンを取得
        Scene currentScene = SceneManager.GetActiveScene();

        // 取得したシーンのビルドインデックスを使ってシーンをロードする
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
