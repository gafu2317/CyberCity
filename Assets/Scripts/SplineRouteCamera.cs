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
  [Header("ループ時スピード設定")]
  public bool useLoopSpeedCurve = true;
  public AnimationCurve loopSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);
  [Range(0.1f, 5f)]
  public float splineTension = 1f;
  public int splineResolution = 100;
  public bool useConstantSpeed = true;
  public float speedMultiplier = 1f;

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
      // 修正：スプライン計算と一致する開始位置を設定
      Vector3 startPosition;
      if (splinePoints != null && splinePoints.Length > 0)
      {
        startPosition = GetSplinePosition(0f);
      }
      else
      {
        startPosition = waypoints[0].position;
      }
      
      transform.position = startPosition;
      Debug.Log($"[スタート設定] 開始位置: {startPosition}");
    }

    if (autoStart)
    {
      StartMovement();
    }
  }

  void Update()
  {
    if (!isMoving || splinePoints == null || splinePoints.Length == 0) return;

    // 時間管理
    currentTime += Time.deltaTime;
    float normalizedTime = currentTime / totalTravelTime;

    // ループ処理
    if (loop)
    {
      normalizedTime = normalizedTime % 1f;  // 0-1で循環
    }
    else if (normalizedTime >= 1f)
    {
      isMoving = false;
      normalizedTime = 1f;
    }

    // スピードカーブ適用（ループ停止防止）
    float curvedTime;
    if (loop && useLoopSpeedCurve)
    {
      curvedTime = loopSpeedCurve.Evaluate(normalizedTime);  // Linearカーブで停止防止
    }
    else
    {
      curvedTime = speedCurve.Evaluate(normalizedTime);
    }
    
    // 位置計算
    Vector3 newPosition, lookDirection;

    if (useConstantSpeed)
    {
      float targetDistance = curvedTime * totalSplineLength;
      newPosition = GetSplinePositionByDistance(targetDistance);
      lookDirection = GetSplineTangentByDistance(targetDistance);
    }
    else
    {
      newPosition = GetSplinePosition(curvedTime);
      lookDirection = GetSplineTangent(curvedTime);
    }

    // カメラ更新
    transform.position = newPosition;
    HandleSplineRotation(lookDirection, normalizedTime);

    if (showDebugInfo && Time.frameCount % 60 == 0)
    {
      float progress = normalizedTime * 100f;
      string mode = useConstantSpeed ? "一定速度" : "時間ベース";
      Debug.Log($"[SplineCamera] 進行: {progress:F1}% | モード: {mode}");
    }
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

    int segments = loop ? waypoints.Length : waypoints.Length - 1;
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

    if (!loop)
    {
      splinePointList.Add(waypoints[waypoints.Length - 1].position);
    }

    splinePoints = splinePointList.ToArray();
    CalculateSplineLength();

    if (showDebugInfo)
    {
      string loopStatus = loop ? "ループあり" : "ループなし";
      Debug.Log($"[SplineCamera] スプライン生成完了: {splinePoints.Length}点, 全長: {totalSplineLength:F2} ({loopStatus})");
    }
  }

  // スプライン用制御点準備
  Vector3[] PrepareControlPoints()
  {
    List<Vector3> points = new List<Vector3>();

    if (loop && waypoints.Length >= 3)
    {
      // ループ用: [last, w0, w1, ..., wN, w0, w1]
      points.Add(waypoints[waypoints.Length - 1].position);

      foreach (Transform waypoint in waypoints)
      {
        points.Add(waypoint.position);
      }
      points.Add(waypoints[0].position);
      points.Add(waypoints[1].position);
    }
    else
    {
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

    if (loop)
    {
      float exactIndex = normalizedTime * splinePoints.Length;
      int index = Mathf.FloorToInt(exactIndex) % splinePoints.Length;
      float fraction = exactIndex - Mathf.FloorToInt(exactIndex);

      int nextIndex = (index + 1) % splinePoints.Length;
      return Vector3.Lerp(splinePoints[index], splinePoints[nextIndex], fraction);
    }
    else
    {
      // 非ループ時の処理
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
  }

  Vector3 GetSplineTangent(float normalizedTime)
  {
    if (splinePoints == null || splinePoints.Length < 2) return Vector3.forward;

    if (loop)
    {
      // ループ時：normalizedTimeは既に0-1の範囲内
      float exactIndex = normalizedTime * splinePoints.Length;
      int index = Mathf.FloorToInt(exactIndex) % splinePoints.Length;
      int nextIndex = (index + 1) % splinePoints.Length;

      return (splinePoints[nextIndex] - splinePoints[index]).normalized;
    }
    else
    {
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
  }

  Vector3 GetSplinePositionByDistance(float targetDistance)
  {
    if (splinePoints == null || splinePoints.Length < 2) return Vector3.zero;

    float accumulatedDistance = 0f;
    int pointCount = loop ? splinePoints.Length : splinePoints.Length - 1;

    for (int i = 0; i < pointCount; i++)
    {
      int nextIndex = loop ? (i + 1) % splinePoints.Length : i + 1;
      float segmentDistance = Vector3.Distance(splinePoints[i], splinePoints[nextIndex]);

      if (accumulatedDistance + segmentDistance >= targetDistance)
      {
        float remainingDistance = targetDistance - accumulatedDistance;
        float segmentRatio = remainingDistance / segmentDistance;

        return Vector3.Lerp(splinePoints[i], splinePoints[nextIndex], segmentRatio);
      }

      accumulatedDistance += segmentDistance;
    }

    return loop ? splinePoints[0] : splinePoints[splinePoints.Length - 1];
  }

  Vector3 GetSplineTangentByDistance(float targetDistance)
  {
    if (splinePoints == null || splinePoints.Length < 2) return Vector3.forward;

    float accumulatedDistance = 0f;
    int pointCount = loop ? splinePoints.Length : splinePoints.Length - 1;

    for (int i = 0; i < pointCount; i++)
    {
      int nextIndex = loop ? (i + 1) % splinePoints.Length : i + 1;
      float segmentDistance = Vector3.Distance(splinePoints[i], splinePoints[nextIndex]);

      if (accumulatedDistance + segmentDistance >= targetDistance)
      {
        return (splinePoints[nextIndex] - splinePoints[i]).normalized;
      }

      accumulatedDistance += segmentDistance;
    }

    int lastIndex = loop ? splinePoints.Length - 1 : splinePoints.Length - 2;
    int lastNextIndex = loop ? 0 : splinePoints.Length - 1;
    return (splinePoints[lastNextIndex] - splinePoints[lastIndex]).normalized;
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

  void CalculateSplineLength()
  {
    totalSplineLength = 0f;
    if (splinePoints == null || splinePoints.Length < 2) return;

    int pointCount = loop ? splinePoints.Length : splinePoints.Length - 1;

    for (int i = 0; i < pointCount; i++)
    {
      int nextIndex = loop ? (i + 1) % splinePoints.Length : i + 1;
      totalSplineLength += Vector3.Distance(splinePoints[i], splinePoints[nextIndex]);
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

  public void AutoFindFromParent()
  {
    if (waypointParent == null) return;

    List<Transform> childWaypoints = new List<Transform>();

    for (int i = 0; i < waypointParent.childCount; i++)
    {
      Transform child = waypointParent.GetChild(i);
      if (child.gameObject.activeInHierarchy)
      {
        childWaypoints.Add(child);
      }
    }

    childWaypoints.Sort((a, b) => CompareWaypointNames(a.name, b.name));

    Transform[] oldWaypoints = waypoints;
    waypoints = childWaypoints.ToArray();

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

    if (name == "GameObject")
    {
      return 0;
    }

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

    int lineCount = loop ? splinePoints.Length : splinePoints.Length - 1;

    for (int i = 0; i < lineCount; i++)
    {
      int nextIndex = loop ? (i + 1) % splinePoints.Length : i + 1;
      Gizmos.DrawLine(splinePoints[i], splinePoints[nextIndex]);
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

    Gizmos.color = Color.cyan;
    Vector3 forward = transform.forward;
    Vector3 arrowEnd = cameraPos + forward * 5f;
    Gizmos.DrawLine(cameraPos, arrowEnd);

    Vector3 right = transform.right * 0.5f;
    Vector3 arrowTip1 = arrowEnd - forward * 1f + right;
    Vector3 arrowTip2 = arrowEnd - forward * 1f - right;
    Gizmos.DrawLine(arrowEnd, arrowTip1);
    Gizmos.DrawLine(arrowEnd, arrowTip2);

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
    if (loop) normalizedTime = normalizedTime % 1f;
    
    float curvedTime;
    if (loop && useLoopSpeedCurve)
    {
      // ループ時は専用スピードカーブ（停止を防ぐため）
      curvedTime = loopSpeedCurve.Evaluate(normalizedTime);
    }
    else
    {
      curvedTime = speedCurve.Evaluate(normalizedTime);
    }

    if (loop)
    {
      // ループ時のウェイポイントインデックス計算
      return Mathf.FloorToInt(curvedTime * waypoints.Length) % waypoints.Length;
    }
    else
    {
      return Mathf.FloorToInt(curvedTime * (waypoints.Length - 1));
    }
  }
}