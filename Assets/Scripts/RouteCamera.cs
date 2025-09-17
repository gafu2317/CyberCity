using System.Collections.Generic;
using UnityEngine;

public class RouteCamera : MonoBehaviour
{
  [Header("基本設定")]
  public Transform[] waypoints; // 目印を入れる箱
  public float moveSpeed = 50f; //移動の速さ
  public float rotationSpeed = 2f; //回転の速さ
  [Header("親からの自動取得")]
  public Transform waypointParent;  // ウェイポイントの親オブジェクト
  public bool autoUpdateInEditor = true; // エディタで自動更新するか
  [Header("動作設定")]
  public bool loop = false; //ループするかどうか
  public bool autoStart = true; //自動でスタートするかどうか
  public float arrivalThreshold = 0.1f; //目標に到達したと見なす距離
  [Header("デバッグ設定")]
  public bool showDebugInfo = true; //デバッグ情報を表示するかどうか
  public bool showGizmos = true; //シーンビューでルートを表示するか
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
    for (int i = 0; i < waypoints.Length; i++)
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

  // カメラの向きを制御する処理
  void HandleCameraRotation()
  {
    int nextWaypointIndex = currentIndex + 1;
    if (nextWaypointIndex < waypoints.Length)
    {
      // まだ次のウェイポイントがある場合：次のウェイポイントを見る
      Vector3 nextWaypointPosition = waypoints[nextWaypointIndex].position;
      Vector3 lookDirection = nextWaypointPosition - transform.position;

      if (lookDirection.magnitude > 0.01f)
      {
        lookDirection = lookDirection.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
      }

      if (showDebugInfo && Time.frameCount % 60 == 0)
      {
        Debug.Log($"[カメラ向き] 次のウェイポイント[{nextWaypointIndex}]を注視中");
      }
    }
    else
    {
      // 最後のウェイポイントに向かっている場合：回転しない
      if (showDebugInfo && Time.frameCount % 60 == 0)
      {
        Debug.Log("[カメラ向き] 最後のウェイポイント - 視線回転なし");
      }

      // 何もしない（transform.rotationを変更しない）
      // カメラは最後の向きのまま最後のウェイポイントに向かう
    }
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

  void OnValidate()
  {
    //エディタでのみ実行
    if (!Application.isPlaying && autoUpdateInEditor && waypointParent != null)
    {
      AutoFindFromParent();
    }
  }

  public void AutoFindFromParent()
  {
    if (waypointParent == null)
    {
      return; // エラーメッセージは出さない（エディタ作業中のため）
    }

    List<Transform> childWaypoints = new List<Transform>();

    // 親の子をすべて取得
    for (int i = 0; i < waypointParent.childCount; i++)
    {
      Transform child = waypointParent.GetChild(i);

      // アクティブな子のみ追加
      if (child.gameObject.activeInHierarchy)
      {
        childWaypoints.Add(child);
      }
    }

    // 【修正】数値ソートに変更
    childWaypoints.Sort((a, b) => CompareWaypointNames(a.name, b.name));

    waypoints = childWaypoints.ToArray();

    if (Application.isPlaying == false && showDebugInfo)
    {
      Debug.Log($"[RouteCamera] エディタで {waypoints.Length}個のウェイポイントを更新");
    }
  }

  // 手動での更新メソッド（Inspectorボタン用）
  [ContextMenu("ウェイポイントを取得")]
  public void ManualRefreshWaypoints()
  {
    AutoFindFromParent();
    Debug.Log($"[RouteCamera] 手動で {waypoints.Length}個のウェイポイントを取得しました");

    // 取得したウェイポイントの一覧を表示
    for (int i = 0; i < waypoints.Length; i++)
    {
      Debug.Log($"  [{i}] {waypoints[i].name}");
    }
  }

  //シーンビューでルートを可視化
  void OnDrawGizmos()
  {
    if (!showGizmos) return;

    // waypoints配列が空でwaypointParentが設定されている場合の警告表示
    if ((waypoints == null || waypoints.Length == 0) && waypointParent != null)
    {
      Gizmos.color = Color.red;
      if (waypointParent != null)
      {
        Gizmos.DrawWireCube(waypointParent.position, Vector3.one * 2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(waypointParent.position + Vector3.up * 1.5f,
            "Waypoint Parent\n(配列が空です)");
#endif
      }
      return;
    }

    if (waypoints == null || waypoints.Length < 2) return;

    // ルートを線で描画
    Gizmos.color = Color.yellow;
    for (int i = 0; i < waypoints.Length - 1; i++)
    {
      if (waypoints[i] != null && waypoints[i + 1] != null)
      {
        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
      }
    }

    // 各ウェイポイントを球で描画
    for (int i = 0; i < waypoints.Length; i++)
    {
      if (waypoints[i] != null)
      {
        // 実行中は現在のウェイポイントを強調
        if (Application.isPlaying)
        {
          Gizmos.color = (i == currentIndex) ? Color.red : Color.green;
        }
        else
        {
          // エディタ時は全て緑
          Gizmos.color = Color.green;
        }

        Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);

        // 番号を表示
#if UNITY_EDITOR
        UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 0.8f, $"{i}");
#endif
      }
    }

    // 実行中のカメラ情報表示
    if (Application.isPlaying)
    {
      // カメラの向き表示（以前と同じ）
      Gizmos.color = Color.blue;
      Vector3 cameraPosition = transform.position;
      Vector3 forwardDirection = transform.forward;
      Vector3 arrowEnd = cameraPosition + forwardDirection * 4f;

      Gizmos.DrawLine(cameraPosition, arrowEnd);

      // 次のウェイポイントへの視線
      int nextIndex = currentIndex + 1;
      if (nextIndex < waypoints.Length && waypoints[nextIndex] != null)
      {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(cameraPosition, waypoints[nextIndex].position);
        Gizmos.DrawWireSphere(waypoints[nextIndex].position, 1f);
      }
    }
  }
  // 【新機能】ウェイポイント名の数値比較
  int CompareWaypointNames(string nameA, string nameB)
  {
    // 名前から数値部分を抽出
    int numberA = ExtractNumberFromName(nameA);
    int numberB = ExtractNumberFromName(nameB);

    // 数値で比較
    if (numberA != numberB)
    {
      return numberA.CompareTo(numberB);
    }

    // 数値が同じ場合は文字列で比較（念のため）
    return string.Compare(nameA, nameB);
  }
  int ExtractNumberFromName(string name)
  {
    // 正規表現を使わないシンプルな方法
    string numberPart = "";
    bool foundNumber = false;

    // 名前を後ろから見て、連続する数字を取得
    for (int i = name.Length - 1; i >= 0; i--)
    {
      char c = name[i];

      if (char.IsDigit(c))
      {
        numberPart = c + numberPart;
        foundNumber = true;
      }
      else if (foundNumber)
      {
        // 数字の塊が見つかったので終了
        break;
      }
    }

    // 数値に変換
    if (foundNumber && int.TryParse(numberPart, out int result))
    {
      return result;
    }

    // フォールバック：名前のハッシュを使う
    return name.GetHashCode();
  }
}

