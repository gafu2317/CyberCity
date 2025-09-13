using UnityEngine;
using System.Collections.Generic;
using System;

public class SplineRouteCamera : MonoBehaviour
{
  [Header("基本設定")]
  public Transform[] waypoints;
  public Transform lookAtTarget;
  public float rotationSpeed = 2f;

  [Header("親からの自動取得")]
  public Transform waypointParent;
  public bool autoUpdateInEditor = true;

  [Header("スプライン設定")]
  public float totalTravelTime = 30f;
  public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  [Range(0.1f, 5f)]
  public float splineTension = 1f;
  public int splineResolution = 100;
  public bool useConstantSpeed = true;  // 【新追加】一定速度モード
  public float speedMultiplier = 1f;    // 【新追加】速度倍率

  [Header("動作設定")]
  public bool loop = false;
  public bool autoStart = true;

  [Header("デバッグ設定")]
  public bool showDebugInfo = true;
  public bool showGizmos = true;
  public bool showSplinePath = true;
  public bool showSplinePoints = false;

  private float currentTime = 0f;
  private bool isMoving = false;
  private Vector3[] splinePoints;
  private float totalSplineLength = 0f;
  

  void Start()
  {
    if (!ValidateSetup())
    {
      enabled = false;
      return;
    }

    GenerateSpline();

    if (waypoints != null && waypoints.Length > 0)
    {
      Vector3 intendedStart = waypoints[0].position;

      // 【新機能】スプライン内でWP0に最も近い点を見つける
      int startIndex = FindClosestSplineIndex(waypoints[0].position);
      Vector3 actualStart = splinePoints[startIndex];

      Debug.Log($"[スタート位置] 想定: {intendedStart}, 実際のスプライン開始: {actualStart}");
      Debug.Log($"[スプライン開始インデックス] {startIndex} / {splinePoints.Length}");

      transform.position = actualStart; // スプライン上の正確な位置から開始
    }

    if (autoStart)
    {
      StartMovement();
    }
  }

  // 【新機能】指定位置に最も近いスプライン点のインデックスを検索
  int FindClosestSplineIndex(Vector3 targetPosition)
  {
    if (splinePoints == null || splinePoints.Length == 0) return 0;

    int closestIndex = 0;
    float minDistance = Vector3.Distance(splinePoints[0], targetPosition);

    for (int i = 1; i < splinePoints.Length; i++)
    {
      float distance = Vector3.Distance(splinePoints[i], targetPosition);
      if (distance < minDistance)
      {
        minDistance = distance;
        closestIndex = i;
      }
    }

    return closestIndex;
  }

  void Update()
  {
    if (!isMoving || splinePoints == null || splinePoints.Length == 0) return;

    currentTime += Time.deltaTime;
    float normalizedTime = currentTime / totalTravelTime;

    // 【修正】滑らかなループ処理
    if (normalizedTime >= 1f)
    {
      if (loop)
      {
        // 1.0を超えた分を次のループに持ち越す
        while (normalizedTime >= 1f)
        {
          normalizedTime -= 1f;
        }
        currentTime = normalizedTime * totalTravelTime;
      }
      else
      {
        isMoving = false;
        normalizedTime = 1f;
      }
    }

    Vector3 newPosition, lookDirection;

    if (useConstantSpeed)
    {
      // 一定速度モード
      float curvedTime = speedCurve.Evaluate(normalizedTime);
      float targetDistance = curvedTime * totalSplineLength;
      newPosition = GetSplinePositionByDistance(targetDistance);
      lookDirection = GetSplineTangentByDistance(targetDistance);
    }
    else
    {
      // 従来の時間ベースモード
      float curvedTime = speedCurve.Evaluate(normalizedTime);
      newPosition = GetSplinePosition(curvedTime);
      lookDirection = GetSplineTangent(curvedTime);
    }

    transform.position = newPosition;
    HandleSplineRotation(lookDirection, normalizedTime);

    if (showDebugInfo && Time.frameCount % 60 == 0)
    {
      float progress = normalizedTime * 100f;
      string mode = useConstantSpeed ? "一定速度" : "時間ベース";
      Debug.Log($"[SplineCamera] 進行: {progress:F1}% | モード: {mode}");
    }
  }


  // 【新機能】距離ベースでスプライン上の位置を取得
  Vector3 GetSplinePositionByDistance(float targetDistance)
  {
    if (splinePoints == null || splinePoints.Length < 2) return Vector3.zero;

    float accumulatedDistance = 0f;

    // 目標距離に到達する区間を見つける
    for (int i = 0; i < splinePoints.Length - 1; i++)
    {
      float segmentDistance = Vector3.Distance(splinePoints[i], splinePoints[i + 1]);

      if (accumulatedDistance + segmentDistance >= targetDistance)
      {
        // この区間内に目標距離がある
        float remainingDistance = targetDistance - accumulatedDistance;
        float segmentRatio = remainingDistance / segmentDistance;

        return Vector3.Lerp(splinePoints[i], splinePoints[i + 1], segmentRatio);
      }

      accumulatedDistance += segmentDistance;
    }

    // 最後の点を返す
    return splinePoints[splinePoints.Length - 1];
  }

  // 【新機能】距離ベースでスプラインの接線を取得
  Vector3 GetSplineTangentByDistance(float targetDistance)
  {
    if (splinePoints == null || splinePoints.Length < 2) return Vector3.forward;

    float accumulatedDistance = 0f;

    // 目標距離に到達する区間を見つける
    for (int i = 0; i < splinePoints.Length - 1; i++)
    {
      float segmentDistance = Vector3.Distance(splinePoints[i], splinePoints[i + 1]);

      if (accumulatedDistance + segmentDistance >= targetDistance)
      {
        // この区間の接線方向を返す
        return (splinePoints[i + 1] - splinePoints[i]).normalized;
      }

      accumulatedDistance += segmentDistance;
    }

    // 最後の区間の接線を返す
    int lastIndex = splinePoints.Length - 2;
    return (splinePoints[lastIndex + 1] - splinePoints[lastIndex]).normalized;
  }
  void GenerateSpline()
  {
    if (waypoints == null || waypoints.Length < 2)
    {
      Debug.LogError("[SplineCamera] 最低2個のウェイポイントが必要です");
      return;
    }

    Vector3[] controlPoints = PrepareControlPoints();
    List<Vector3> splinePointList = new List<Vector3>();

    int segments = controlPoints.Length - 3;
    int pointsPerSegment = Mathf.Max(4, splineResolution / segments);

    for (int i = 0; i < segments; i++)
    {
      for (int j = 0; j < pointsPerSegment; j++)
      {
        float t = (float)j / pointsPerSegment;
        Vector3 point = CatmullRomSpline(
            controlPoints[i],
            controlPoints[i + 1],
            controlPoints[i + 2],
            controlPoints[i + 3],
            t
        );
        splinePointList.Add(point);
      }
    }

    splinePointList.Add(controlPoints[controlPoints.Length - 2]);
    splinePoints = splinePointList.ToArray();
    CalculateSplineLength();

    if (showDebugInfo)
    {
      string loopStatus = loop ? "ループあり" : "ループなし";
      Debug.Log($"[SplineCamera] スプライン生成完了: {splinePoints.Length}点, 全長: {totalSplineLength:F2} ({loopStatus})");
    }
  }

  Vector3[] PrepareControlPoints()
  {
    List<Vector3> points = new List<Vector3>();

    if (loop && waypoints.Length >= 3)
    {
      // 【滑らかなループ用】最後→最初への自然な接続
      points.Add(waypoints[waypoints.Length - 2].position);  // WP41
      points.Add(waypoints[waypoints.Length - 1].position);  // WP42

      foreach (Transform waypoint in waypoints)
      {
        points.Add(waypoint.position);  // WP0, WP1, ..., WP42
      }

      points.Add(waypoints[0].position);  // WP0（ループ接続）
      points.Add(waypoints[1].position);  // WP1
    }
    else
    {
      // 非ループ時
      points.Add(waypoints[0].position);

      foreach (Transform waypoint in waypoints)
      {
        points.Add(waypoint.position);
      }

      points.Add(waypoints[waypoints.Length - 1].position);
    }

    return points.ToArray();
  }

  Vector3 CatmullRomSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
  {
    float t2 = t * t;
    float t3 = t2 * t;

    return 0.5f * (
        (2f * p1) +
        (-p0 + p2) * t +
        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
        (-p0 + 3f * p1 - 3f * p2 + p3) * t3
    ) * splineTension;
  }

  Vector3 GetSplinePosition(float normalizedTime)
  {
    if (splinePoints == null || splinePoints.Length == 0) return Vector3.zero;

    float exactIndex = normalizedTime * (splinePoints.Length - 1);
    int index = Mathf.FloorToInt(exactIndex);
    float fraction = exactIndex - index;

    if (index >= splinePoints.Length - 1)
    {
      return splinePoints[splinePoints.Length - 1];
    }
    if (index < 0)
    {
      return splinePoints[0];
    }

    return Vector3.Lerp(splinePoints[index], splinePoints[index + 1], fraction);
  }

  Vector3 GetSplineTangent(float normalizedTime)
  {
    if (splinePoints == null || splinePoints.Length < 2) return Vector3.forward;

    float exactIndex = normalizedTime * (splinePoints.Length - 1);
    int index = Mathf.FloorToInt(exactIndex);

    if (index >= splinePoints.Length - 2)
    {
      index = splinePoints.Length - 2;
    }
    if (index < 0)
    {
      index = 0;
    }

    Vector3 tangent = (splinePoints[index + 1] - splinePoints[index]).normalized;
    return tangent;
  }

  void HandleSplineRotation(Vector3 moveDirection, float normalizedTime)
  {
    if (lookAtTarget != null)
    {
      Vector3 lookDirection = (lookAtTarget.position - transform.position).normalized;
      Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
      transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    else
    {
      if (moveDirection.magnitude > 0.01f)
      {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
      }
    }
  }

  // 【改良】より正確な全長計算
  void CalculateSplineLength()
  {
    totalSplineLength = 0f;
    if (splinePoints == null || splinePoints.Length < 2) return;

    for (int i = 0; i < splinePoints.Length - 1; i++)
    {
      totalSplineLength += Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
    }

    if (showDebugInfo)
    {
      Debug.Log($"[SplineCamera] スプライン全長: {totalSplineLength:F2}");
    }
  }

  public void StartMovement()
  {
    currentTime = 0f;
    isMoving = true;
  }

  public void StopMovement()
  {
    isMoving = false;
  }

  public void RegenerateSpline()
  {
    GenerateSpline();
  }

  void OnValidate()
  {
    if (!Application.isPlaying && autoUpdateInEditor && waypointParent != null)
    {
      AutoFindFromParent();

      if (waypoints != null && waypoints.Length >= 2)
      {
        GenerateSpline();
      }
    }
  }

  // 【修正】全ての子を取得（名前判定なし）
  public void AutoFindFromParent()
  {
    if (waypointParent == null) return;

    List<Transform> childWaypoints = new List<Transform>();

    // 直接の子のみを取得
    for (int i = 0; i < waypointParent.childCount; i++)
    {
      Transform child = waypointParent.GetChild(i);
      if (child.gameObject.activeInHierarchy)
      {
        childWaypoints.Add(child);
      }
    }

    // 数値ソート
    childWaypoints.Sort((a, b) => CompareWaypointNames(a.name, b.name));

    Transform[] oldWaypoints = waypoints;
    waypoints = childWaypoints.ToArray();

    // 変更があった場合のみログ出力
    if (!ArraysEqual(oldWaypoints, waypoints))
    {
      if (Application.isPlaying == false && showDebugInfo)
      {
        Debug.Log($"[SplineCamera] ウェイポイント更新: {waypoints.Length}個");
        for (int i = 0; i < Mathf.Min(waypoints.Length, 5); i++)
        {
          Debug.Log($"  [{i}] {waypoints[i].name}");
        }
        if (waypoints.Length > 5)
        {
          Debug.Log($"  ... 他 {waypoints.Length - 5}個");
        }
      }
    }
  }

  bool ArraysEqual(Transform[] array1, Transform[] array2)
  {
    if (array1 == null && array2 == null) return true;
    if (array1 == null || array2 == null) return false;
    if (array1.Length != array2.Length) return false;

    for (int i = 0; i < array1.Length; i++)
    {
      if (array1[i] != array2[i]) return false;
    }
    return true;
  }

  int CompareWaypointNames(string nameA, string nameB)
  {
    int numberA = ExtractNumberFromName(nameA);
    int numberB = ExtractNumberFromName(nameB);

    if (numberA != numberB)
    {
      return numberA.CompareTo(numberB);
    }
    return string.Compare(nameA, nameB);
  }

  int ExtractNumberFromName(string name)
  {
    // GameObject (1), GameObject (2) の形式をチェック
    if (name.Contains("(") && name.Contains(")"))
    {
      int startIndex = name.IndexOf("(") + 1;
      int endIndex = name.IndexOf(")");

      if (startIndex < endIndex)
      {
        string numberPart = name.Substring(startIndex, endIndex - startIndex);
        if (int.TryParse(numberPart, out int result))
        {
          return result;
        }
      }
    }

    // 通常の数値抽出（末尾から）
    string numberPart2 = "";
    for (int i = name.Length - 1; i >= 0; i--)
    {
      char c = name[i];
      if (char.IsDigit(c))
      {
        numberPart2 = c + numberPart2;
      }
      else
      {
        break;
      }
    }

    if (numberPart2.Length > 0 && int.TryParse(numberPart2, out int result2))
    {
      return result2;
    }

    // GameObject（番号なし）は0として扱う
    if (name == "GameObject")
    {
      return 0;
    }

    // 最後の手段：ハッシュ値
    return Math.Abs(name.GetHashCode()) % 1000;
  }

  bool ValidateSetup()
  {
    if (waypoints == null || waypoints.Length < 2)
    {
      Debug.LogWarning("[SplineCamera] 最低2個のウェイポイントが必要です");
      return false;
    }

    for (int i = 0; i < waypoints.Length; i++)
    {
      if (waypoints[i] == null)
      {
        Debug.LogError($"[SplineCamera] waypoints[{i}]がnullです");
        return false;
      }
    }
    return true;
  }

  [ContextMenu("ウェイポイントを強制更新")]
  public void ForceRefreshWaypoints()
  {
    AutoFindFromParent();

    if (waypoints != null && waypoints.Length >= 2)
    {
      GenerateSpline();
      Debug.Log($"[SplineCamera] 強制更新完了: {waypoints.Length}個のウェイポイント");
    }
    else
    {
      Debug.LogWarning("[SplineCamera] ウェイポイントが足りません（最低2個必要）");
    }
  }

  void OnDrawGizmos()
  {
    if (!showGizmos) return;

    DrawWaypoints();
    DrawSplinePath();

    if (Application.isPlaying)
    {
      DrawRuntimeInfo();
    }
  }

  void DrawWaypoints()
  {
    if (waypoints == null || waypoints.Length == 0) return;

    for (int i = 0; i < waypoints.Length; i++)
    {
      if (waypoints[i] == null) continue;

      Vector3 pos = waypoints[i].position;

      if (Application.isPlaying)
      {
        int currentWaypointIndex = GetCurrentWaypointIndex();
        if (i == currentWaypointIndex)
          Gizmos.color = Color.red;
        else if (i < currentWaypointIndex)
          Gizmos.color = Color.gray;
        else
          Gizmos.color = Color.green;
      }
      else
      {
        Gizmos.color = i == 0 ? Color.green : (i == waypoints.Length - 1 ? Color.red : Color.yellow);
      }

      Gizmos.DrawWireSphere(pos, 0.8f);

#if UNITY_EDITOR
      UnityEditor.Handles.Label(pos + Vector3.up * 1.2f, $"{i}");
#endif
    }
  }

  void DrawSplinePath()
  {
    if (!showSplinePath) return;

    if (!Application.isPlaying && waypoints != null && waypoints.Length >= 2)
    {
      GenerateSpline();
    }

    if (splinePoints == null || splinePoints.Length < 2) return;

    Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
    for (int i = 0; i < splinePoints.Length - 1; i++)
    {
      Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
    }

    if (showSplinePoints)
    {
      Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
      foreach (Vector3 point in splinePoints)
      {
        Gizmos.DrawWireSphere(point, 0.1f);
      }
    }
  }

  void DrawRuntimeInfo()
  {
    if (splinePoints == null || splinePoints.Length == 0) return;

    Vector3 cameraPos = transform.position;

    // カメラの進行方向矢印
    Gizmos.color = Color.cyan;
    Vector3 forward = transform.forward;
    Vector3 arrowEnd = cameraPos + forward * 5f;
    Gizmos.DrawLine(cameraPos, arrowEnd);

    Vector3 right = transform.right * 0.5f;
    Vector3 arrowTip1 = arrowEnd - forward * 1f + right;
    Vector3 arrowTip2 = arrowEnd - forward * 1f - right;
    Gizmos.DrawLine(arrowEnd, arrowTip1);
    Gizmos.DrawLine(arrowEnd, arrowTip2);

    // lookAtTargetへの線
    if (lookAtTarget != null)
    {
      Gizmos.color = new Color(1, 0, 1, 0.5f);
      Gizmos.DrawLine(cameraPos, lookAtTarget.position);
      Gizmos.DrawWireSphere(lookAtTarget.position, 0.5f);
    }
  }

  int GetCurrentWaypointIndex()
  {
    if (splinePoints == null || splinePoints.Length == 0) return 0;

    float normalizedTime = currentTime / totalTravelTime;
    float curvedTime = speedCurve.Evaluate(normalizedTime);

    return Mathf.FloorToInt(curvedTime * (waypoints.Length - 1));
  }
}