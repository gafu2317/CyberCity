using UnityEngine;

public class RouteCamera : MonoBehaviour
{
  [Header("基本設定")]
  public Transform[] waypoints; // 目印を入れる箱
  public float moveSpeed = 5f; //移動の速さ
  public Transform lookAtTarget;  //見続ける対象
  public float rotationSpeed = 2f; //回転の速さ
  [Header("動作設定")]
  public bool loop = false; //ループするかどうか
  public bool autoStart = true; //自動でスタートするかどうか
  public float arrivalThreshold = 0.1f; //目標に到達したと見なす距離
  [Header("デバッグ設定")]
  public bool showDebugInfo = true; //デバッグ情報を表示するかどうか
  public bool ShowGizmos = true; //シーンビューでルートを表示するか
  private int currentIndex = 0; //今向かっている目印の番号
  private bool isMoving = false; //移動中かどうか
  private float totalDistance = 0f; //総移動距離(デバッグ用)

  void Start()
  {
    //重要：初期化時のエラーチェック
    if (!ValidateSetup())
    {
      Debug.LogError($"[RouteCamera]セットアップエラー：{gameObject.name}を確認してください。");
      enabled = false; //このスクリプトを無効化
      return;
    }
    if (waypoints.Length > 0)
    {
      transform.position = waypoints[0].position;
      //デバッグ情報
      if (showDebugInfo)
      {
        CalculateTotalDistance();
      }
    }

    // 自動でスタートする場合
    if (autoStart)
    {
      StartMovement();
    }
  }

  void Update()
  {
    if (!isMoving) return; //移動していないならば何もしない


    // 現在の目標目標を取得
    Transform targetWaypoint = waypoints[currentIndex];
    Vector3 targetPosition = targetWaypoint.position;

    //目標までの距離を計算
    float distance = Vector3.Distance(transform.position, targetPosition);

    // デバッグ情報の表示
    if (showDebugInfo)
    {
      DisplayDebugInfo(distance);
    }

    //目標に近づいたら次の目印へ
    if (distance < arrivalThreshold)
    {
      OnWaypointReached(); //目印到達時の処理
      currentIndex++;
      //全ての目標を回り終わったかチェック
      if (currentIndex >= waypoints.Length)
      {
        HandleRouteCompletion(); //ルート完了処理
        return;
      }
    }

    //目標に向かって移動  
    Vector3 direction = (targetPosition - transform.position).normalized;
    transform.position += direction * moveSpeed * Time.deltaTime;

    //カメラの向きを制御
    HandleCameraRotation();
  }

  //初期設定の妥当性をチェック
  bool ValidateSetup()
  {
    if (waypoints == null || waypoints.Length == 0)
    {
      Debug.LogError("[RouteCamera] 目印が設定されていません。");
      return false;
    }
    for(int i = 0; i < waypoints.Length; i++)
    {
      if (waypoints[i] == null)
      {
        Debug.LogError($"[RouteCamera] 目印の配列にnullが含まれています。インデックス: {i}");
        return false;
      }
    }
    return true;
  }

  //総距離を事前計算
  void CalculateTotalDistance()
  {
    totalDistance = 0f;
    for (int i = 0; i < waypoints.Length - 1; i++)
    {
      totalDistance += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
    }
    Debug.Log($"[RouteCamera] 総移動距離: {totalDistance:F2}");
  }

  //デバッグ情報の表示
  void DisplayDebugInfo(float currentDistance)
  {
    //コンソールに定期的に情報を出力
    if (Time.frameCount % 60 == 0) //約1秒ごと
    {
      float progress = (float)currentIndex / waypoints.Length * 100f;
      Debug.Log($"[RouteCamera]進行状況: {progress:F1}% | 現在目標:{currentIndex} | 距離:{currentDistance:F2}");
    }
  }

  //ウィとポイント到達時の処理
  void OnWaypointReached()
  {
    if (showDebugInfo)
    {
      Debug.Log($"[RouteCamera] 目印 {currentIndex} に到達しました。");
    }
  }
  //シーンビューでルートを可視化
  void OnDrawGizmos()
  {

    if (!ShowGizmos || waypoints == null || waypoints.Length == 0) return;
    Gizmos.color = Color.yellow;
    for (int i = 0; i < waypoints.Length - 1; i++)
    {
      if (waypoints[i] != null && waypoints[i + 1] != null)
      {
        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
      }
    }
    //各目印に球を描画
    for (int i = 0; i < waypoints.Length; i++)
    {
      if (waypoints[i] != null)
      {
        Gizmos.color = (i == currentIndex) ? Color.red : Color.green;
        Gizmos.DrawSphere(waypoints[i].position, 0.5f);
      }
    }
  }

  // カメラの向きを制御する処理
  void HandleCameraRotation()
  {
    //見る対象が設定されている場合のみ実行
    if (lookAtTarget == null) return;
    //カメラから対象へ方向を計算
    Vector3 lookDirection = lookAtTarget.position - transform.position;
    // その方向を向くために回転を計算
    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
    //スムーズに回転させる
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
  }

  //移動を開始する処理
  public void StartMovement()
  {
    isMoving = true;
    currentIndex = 0; //最初の目印にリセット 
  }

  //移動を停止する処理
  public void StopMovement()
  {
    isMoving = false;
  }

  //ルート完了時の処理
  void HandleRouteCompletion()
  {
    if (loop)
    {
      currentIndex = 0; //最初の目印に戻る
    }
    else
    {
      isMoving = false; //移動を停止
    }
  }
}