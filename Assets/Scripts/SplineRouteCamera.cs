using UnityEngine;
using System.Collections.Generic;
using System;

// Inspector用分岐設定
[System.Serializable]
public class BranchChoice
{
  [Tooltip("分岐させるノードのインデックス")]
  public int nodeIndex;
  
  [Tooltip("選択する分岐のインデックス")]
  public int branchIndex;
  
  [Tooltip("この設定の説明")]
  public string description = "";
}

// 分岐ポイント用データ構造
[System.Serializable]
public class WaypointNode
{
  [Header("基本ポイント")]
  public Transform mainPoint;
  
  [Header("分岐設定")]
  public Transform[] branches;  // nullまたは空なら通常ポイント
  
  // 分岐点かどうかを判定
  public bool IsBranch => branches != null && branches.Length > 1;
  
  // 選択されたポイントを取得（分岐点でない場合はmainPoint）
  public Transform GetSelectedPoint(int branchIndex = 0)
  {
    if (!IsBranch) return mainPoint;
    
    if (branchIndex < 0 || branchIndex >= branches.Length)
    {
      Debug.LogWarning($"分岐インデックス {branchIndex} が範囲外です。デフォルトを使用します。");
      return branches.Length > 0 ? branches[0] : mainPoint;
    }
    
    return branches[branchIndex];
  }
}

public class SplineRouteCamera : MonoBehaviour
{
  [Header("基本設定")]
  public WaypointNode[] waypointNodes;  // 新しい分岐対応構造
  public Transform lookAtTarget;
  public float rotationSpeed = 2f;
  
  [Header("分岐制御")]
  public bool enableBranching = true;  // 分岐機能の有効/無効
  
  [Header("Inspector分岐設定")]
  [SerializeField] private BranchChoice[] manualBranchChoices;  // Inspector用分岐設定
  
  // 分岐選択のデリゲート（外部制御用）
  // 使用例: camera.branchSelector = (nodeIndex, branchCount) => Random.Range(0, branchCount);
  public delegate int BranchSelector(int nodeIndex, int branchCount);
  public BranchSelector branchSelector;
  
  // デフォルトの分岐選択（最初の選択肢を選ぶ）
  private int DefaultBranchSelector(int nodeIndex, int branchCount)
  {
    return 0;  // 常に最初の分岐を選択
  }
  
  // 現在の分岐選択状況を保存
  private Dictionary<int, int> currentBranchChoices = new Dictionary<int, int>();
  
  // 分岐選択を取得（キャッシュ付き）
  private int GetBranchChoice(int nodeIndex)
  {
    if (!enableBranching) return 0;
    if (waypointNodes == null || nodeIndex >= waypointNodes.Length) return 0;
    
    var node = waypointNodes[nodeIndex];
    if (!node.IsBranch) return 0;
    
    // キャッシュから取得を試行
    if (currentBranchChoices.ContainsKey(nodeIndex))
    {
      return currentBranchChoices[nodeIndex];
    }
    
    // 1. Inspector設定を優先チェック
    if (manualBranchChoices != null)
    {
      foreach (var choice in manualBranchChoices)
      {
        if (choice.nodeIndex == nodeIndex)
        {
          int clampedChoice = Mathf.Clamp(choice.branchIndex, 0, node.branches.Length - 1);
          currentBranchChoices[nodeIndex] = clampedChoice;
          return clampedChoice;
        }
      }
    }
    
    // 2. 外部制御または デフォルト選択
    var selector = branchSelector ?? DefaultBranchSelector;
    int result = selector(nodeIndex, node.branches.Length);
    
    // 範囲チェック
    result = Mathf.Clamp(result, 0, node.branches.Length - 1);
    
    // キャッシュに保存
    currentBranchChoices[nodeIndex] = result;
    
    return result;
  }
  
  // 分岐選択をリセット（ルート再生成時に使用）
  public void ClearBranchChoices()
  {
    currentBranchChoices.Clear();
  }
  
  // 分岐を更新してスプラインを再生成
  public void UpdateBranches()
  {
    ClearBranchChoices();
    GenerateSpline();
  }
  
  // 特定の分岐選択を強制設定
  public void SetBranchChoice(int nodeIndex, int branchIndex)
  {
    if (waypointNodes == null || nodeIndex >= waypointNodes.Length) return;
    if (!waypointNodes[nodeIndex].IsBranch) return;
    
    currentBranchChoices[nodeIndex] = branchIndex;
    UpdateBranches();  // 即座にスプライン再生成
    Debug.Log($"[分岐設定] ノード{nodeIndex}で分岐{branchIndex}を選択");
  }
  
  // テスト用: ランダム分岐選択を設定
  [ContextMenu("ランダム分岐テスト")]
  public void SetRandomBranchSelection()
  {
    branchSelector = (nodeIndex, branchCount) => {
      int choice = UnityEngine.Random.Range(0, branchCount);
      Debug.Log($"[ランダム分岐] ノード{nodeIndex}: {branchCount}個中{choice}番目を選択");
      return choice;
    };
    
    UpdateBranches();
    Debug.Log("[分岐テスト] ランダム分岐選択を有効化しました");
  }
  
  // Inspector用: 分岐設定を生成
  [ContextMenu("分岐設定を自動生成")]
  public void GenerateBranchSettings()
  {
    if (waypointNodes == null) 
    {
      Debug.LogWarning("[分岐設定] ウェイポイントが設定されていません");
      return;
    }
    
    List<BranchChoice> choices = new List<BranchChoice>();
    
    for (int i = 0; i < waypointNodes.Length; i++)
    {
      var node = waypointNodes[i];
      if (node.IsBranch)
      {
        choices.Add(new BranchChoice
        {
          nodeIndex = i,
          branchIndex = 0,
          description = $"{node.mainPoint.name}の分岐選択"
        });
      }
    }
    
    manualBranchChoices = choices.ToArray();
    Debug.Log($"[分岐設定] {choices.Count}個の分岐設定を生成しました");
  }
  
  // Inspector用: 分岐情報を表示
  [ContextMenu("分岐情報を表示")]
  public void ShowBranchInfo()
  {
    if (waypointNodes == null) return;
    
    Debug.Log("=== 分岐情報 ===");
    for (int i = 0; i < waypointNodes.Length; i++)
    {
      var node = waypointNodes[i];
      if (node.IsBranch)
      {
        int selected = GetBranchChoice(i);
        Debug.Log($"ノード[{i}] {node.mainPoint.name}: {node.branches.Length}分岐, 選択中={selected}");
        
        for (int j = 0; j < node.branches.Length; j++)
        {
          string marker = j == selected ? "★" : "　";
          Debug.Log($"  {marker}[{j}] {node.branches[j].name}");
        }
      }
    }
  }

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
  
  [Header("分岐可視化")]
  public bool showBranches = true;  // 分岐点の表示
  public bool showBranchConnections = true;  // 分岐への接続線
  public bool showSelectedBranch = true;  // 選択中分岐の強調表示

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

    if (waypointNodes != null && waypointNodes.Length > 0)
    {
      // 修正：スプライン計算と一致する開始位置を設定
      Vector3 startPosition;
      if (splinePoints != null && splinePoints.Length > 0)
      {
        startPosition = GetSplinePosition(0f);
      }
      else
      {
        startPosition = GetSelectedNodePosition(0);
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
    if (waypointNodes == null || waypointNodes.Length < 2)
    {
      Debug.LogError("[SplineCamera] 最低2個のウェイポイントが必要です");
      return;
    }

    Vector3[] controlPoints = PrepareControlPoints();
    List<Vector3> splinePointList = new List<Vector3>();

    int segments = loop ? waypointNodes.Length : waypointNodes.Length - 1;
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
      splinePointList.Add(GetSelectedNodePosition(waypointNodes.Length - 1));
    }

    splinePoints = splinePointList.ToArray();
    CalculateSplineLength();

    if (showDebugInfo)
    {
      string loopStatus = loop ? "ループあり" : "ループなし";
      Debug.Log($"[SplineCamera] スプライン生成完了: {splinePoints.Length}点, 全長: {totalSplineLength:F2} ({loopStatus})");
    }
  }

  // スプライン用制御点準備（分岐対応）
  Vector3[] PrepareControlPoints()
  {
    List<Vector3> points = new List<Vector3>();

    if (loop && waypointNodes.Length >= 3)
    {
      // ループ用: [last, w0, w1, ..., wN, w0, w1]
      points.Add(GetSelectedNodePosition(waypointNodes.Length - 1));
      
      for (int i = 0; i < waypointNodes.Length; i++)
      {
        points.Add(GetSelectedNodePosition(i));
      }
      
      points.Add(GetSelectedNodePosition(0));
      if (waypointNodes.Length > 1)
        points.Add(GetSelectedNodePosition(1));
    }
    else
    {
      points.Add(GetSelectedNodePosition(0));
      for (int i = 0; i < waypointNodes.Length; i++)
      {
        points.Add(GetSelectedNodePosition(i));
      }
      points.Add(GetSelectedNodePosition(waypointNodes.Length - 1));
    }

    return points.ToArray();
  }
  
  // 選択された分岐の位置を取得
  private Vector3 GetSelectedNodePosition(int nodeIndex)
  {
    if (waypointNodes == null || nodeIndex >= waypointNodes.Length) 
      return Vector3.zero;
      
    var node = waypointNodes[nodeIndex];
    if (!node.IsBranch || !enableBranching)
      return node.mainPoint.position;
      
    int branchChoice = GetBranchChoice(nodeIndex);
    return node.GetSelectedPoint(branchChoice).position;
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
      if (waypointNodes != null && waypointNodes.Length >= 2)
      {
        GenerateSpline();
      }
    }
  }

  public void AutoFindFromParent()
  {
    if (waypointParent == null) return;

    List<WaypointNode> nodeList = new List<WaypointNode>();

    // 親の子をすべて取得
    for (int i = 0; i < waypointParent.childCount; i++)
    {
      Transform child = waypointParent.GetChild(i);

      // アクティブな子のみ処理
      if (!child.gameObject.activeInHierarchy) continue;

      WaypointNode node = new WaypointNode();
      node.mainPoint = child;

      // 子オブジェクトがあれば分岐点として設定
      if (child.childCount > 0)
      {
        List<Transform> branches = new List<Transform>();
        
        for (int j = 0; j < child.childCount; j++)
        {
          Transform branchChild = child.GetChild(j);
          if (branchChild.gameObject.activeInHierarchy)
          {
            branches.Add(branchChild);
          }
        }
        
        // 分岐を名前でソート
        branches.Sort((a, b) => CompareWaypointNames(a.name, b.name));
        node.branches = branches.ToArray();
      }

      nodeList.Add(node);
    }

    // ノードを名前でソート
    nodeList.Sort((a, b) => CompareWaypointNames(a.mainPoint.name, b.mainPoint.name));

    WaypointNode[] oldNodes = waypointNodes;
    waypointNodes = nodeList.ToArray();

    if (!NodesEqual(oldNodes, waypointNodes))
    {
      if (Application.isPlaying == false && showDebugInfo)
      {
        Debug.Log($"[SplineCamera] ウェイポイント更新: {waypointNodes.Length}個");
        for (int i = 0; i < Mathf.Min(waypointNodes.Length, 5); i++)
        {
          var node = waypointNodes[i];
          string branchInfo = node.IsBranch ? $" ({node.branches.Length}分岐)" : "";
          Debug.Log($"  [{i}] {node.mainPoint.name}{branchInfo}");
        }
        if (waypointNodes.Length > 5)
        {
          Debug.Log($"  ... 他 {waypointNodes.Length - 5}個");
        }
      }
    }
  }
  
  // WaypointNode配列の比較
  bool NodesEqual(WaypointNode[] array1, WaypointNode[] array2)
  {
    if (array1 == null && array2 == null) return true;
    if (array1 == null || array2 == null) return false;
    if (array1.Length != array2.Length) return false;

    for (int i = 0; i < array1.Length; i++)
    {
      if (array1[i].mainPoint != array2[i].mainPoint) return false;
      
      // 分岐も比較
      if (array1[i].IsBranch != array2[i].IsBranch) return false;
      if (array1[i].IsBranch)
      {
        if (array1[i].branches.Length != array2[i].branches.Length) return false;
        for (int j = 0; j < array1[i].branches.Length; j++)
        {
          if (array1[i].branches[j] != array2[i].branches[j]) return false;
        }
      }
    }
    return true;
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
    if (waypointNodes == null || waypointNodes.Length < 2)
    {
      Debug.LogWarning("[SplineCamera] 最低2個のウェイポイントが必要です");
      return false;
    }

    for (int i = 0; i < waypointNodes.Length; i++)
    {
      var node = waypointNodes[i];
      if (node.mainPoint == null)
      {
        Debug.LogError($"[SplineCamera] waypointNodes[{i}].mainPointがnullです");
        return false;
      }
      
      // 分岐の検証
      if (node.IsBranch)
      {
        for (int j = 0; j < node.branches.Length; j++)
        {
          if (node.branches[j] == null)
          {
            Debug.LogError($"[SplineCamera] waypointNodes[{i}].branches[{j}]がnullです");
            return false;
          }
        }
      }
    }
    return true;
  }

  [ContextMenu("ウェイポイントを強制更新")]
  public void ForceRefreshWaypoints()
  {
    AutoFindFromParent();

    if (waypointNodes != null && waypointNodes.Length >= 2)
    {
      GenerateSpline();
      Debug.Log($"[SplineCamera] 強制更新完了: {waypointNodes.Length}個のウェイポイント");
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
    if (waypointNodes == null || waypointNodes.Length == 0) return;

    for (int i = 0; i < waypointNodes.Length; i++)
    {
      var node = waypointNodes[i];
      if (node.mainPoint == null) continue;

      Vector3 pos = node.mainPoint.position;

      // メインポイントの色設定
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
        // 分岐点は特別な色で表示
        if (node.IsBranch && showBranches)
        {
          Gizmos.color = Color.magenta;  // 分岐点は紫色
        }
        else
        {
          Gizmos.color = i == 0 ? Color.green : (i == waypointNodes.Length - 1 ? Color.red : Color.yellow);
        }
      }

      // メインポイント描画
      float radius = node.IsBranch ? 1.2f : 0.8f;  // 分岐点は大きく
      Gizmos.DrawWireSphere(pos, radius);

#if UNITY_EDITOR
      string label = node.IsBranch ? $"{i} ({node.branches.Length}分岐)" : $"{i}";
      UnityEditor.Handles.Label(pos + Vector3.up * 1.5f, label);
#endif

      // 分岐点の表示
      if (node.IsBranch && showBranches)
      {
        DrawBranchPoints(i, node);
      }
    }
  }
  
  // 分岐点とその接続を描画
  void DrawBranchPoints(int nodeIndex, WaypointNode node)
  {
    if (!showBranches || node.branches == null) return;
    
    Vector3 mainPos = node.mainPoint.position;
    int selectedBranch = GetBranchChoice(nodeIndex);

    for (int j = 0; j < node.branches.Length; j++)
    {
      if (node.branches[j] == null) continue;
      
      Vector3 branchPos = node.branches[j].position;
      
      // 選択中/未選択で色分け
      if (j == selectedBranch && showSelectedBranch)
      {
        Gizmos.color = Color.cyan;  // 選択中は水色
      }
      else
      {
        Gizmos.color = Color.white;  // 未選択は白
      }
      
      // 分岐点を描画
      Gizmos.DrawWireSphere(branchPos, 0.5f);
      
      // メインポイントとの接続線
      if (showBranchConnections)
      {
        Gizmos.DrawLine(mainPos, branchPos);
      }

#if UNITY_EDITOR
      string branchLabel = j == selectedBranch ? $"[{j}]★" : $"[{j}]";
      UnityEditor.Handles.Label(branchPos + Vector3.up * 0.8f, branchLabel);
#endif
    }
  }

  void DrawSplinePath()
  {
    if (!showSplinePath) return;

    if (!Application.isPlaying && waypointNodes != null && waypointNodes.Length >= 2)
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
      return Mathf.FloorToInt(curvedTime * waypointNodes.Length) % waypointNodes.Length;
    }
    else
    {
      return Mathf.FloorToInt(curvedTime * (waypointNodes.Length - 1));
    }
  }
}