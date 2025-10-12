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
  
  [Header("分岐設定（4つ固定）")]
  public Transform[] branches = new Transform[4];  // 4つ固定
  
  // 分岐点かどうかを判定（有効な分岐が1つ以上ある場合）
  public bool IsBranch => branches != null && branches.Length == 4 && GetValidBranchCount() > 1;
  
  // 有効な分岐数を取得
  public int GetValidBranchCount()
  {
    if (branches == null || branches.Length != 4) return 0;
    
    int count = 0;
    for (int i = 0; i < 4; i++)
    {
      if (branches[i] != null) count++;
    }
    return count;
  }
  
  // 選択されたポイントを取得（分岐点でない場合はmainPoint）
  public Transform GetSelectedPoint(int branchIndex = 0)
  {
    if (!IsBranch) return mainPoint;
    
    // 指定されたインデックスが有効かチェック
    if (branchIndex >= 0 && branchIndex < 4 && branches[branchIndex] != null)
    {
      return branches[branchIndex];
    }
    
    // 指定された分岐が存在しない場合、有効な分岐からランダム選択
    var validBranches = new System.Collections.Generic.List<int>();
    for (int i = 0; i < 4; i++)
    {
      if (branches[i] != null) validBranches.Add(i);
    }
    
    if (validBranches.Count == 0) return mainPoint;
    
    int randomIndex = validBranches[UnityEngine.Random.Range(0, validBranches.Count)];
    
    return branches[randomIndex];
  }
}

public class SplineRouteCamera : MonoBehaviour
{
  [Header("基本設定")]
  public WaypointNode[] waypointNodes;
  public Transform lookAtTarget;
  public float rotationSpeed = 2f;
  
  [Header("分岐制御")]
  [SerializeField] private BranchChoice[] manualBranchChoices;
  
  // シンプルな分岐制御
  private Dictionary<int, int> fixedBranchChoices = new Dictionary<int, int>();
  
  
  // 外部から分岐を指定するメソッド（0-3）
  public void SetNextBranchChoice(int branchIndex)
  {
    if (branchIndex < 0 || branchIndex > 3)
    {
      return;
    }
    
    // 次に来る分岐点を特定して設定
    int nextBranchNode = FindNextBranchNode();
    if (nextBranchNode >= 0)
    {
      fixedBranchChoices[nextBranchNode] = branchIndex;
      GenerateSpline();
    }
  }
  
  // 次の分岐点を特定
  private int FindNextBranchNode()
  {
    if (waypointNodes == null || waypointNodes.Length == 0) return -1;
    
    // 現在位置から順番に分岐点を探す
    int currentIndex = EstimateCurrentWaypointIndex();
    for (int offset = 1; offset < waypointNodes.Length; offset++)
    {
      int targetIndex = (currentIndex + offset) % waypointNodes.Length;
      if (waypointNodes[targetIndex].IsBranch)
      {
        return targetIndex;
      }
    }
    return -1;
  }
  
  // 現在位置推定
  private int EstimateCurrentWaypointIndex()
  {
    if (!isMoving || waypointNodes == null || waypointNodes.Length == 0) return 0;
    float normalizedTime = (currentTime / totalTravelTime) % 1.0f;
    return Mathf.FloorToInt(normalizedTime * waypointNodes.Length) % waypointNodes.Length;
  }
  
  // 分岐選択を取得（シンプル版）
  private int GetBranchChoice(int nodeIndex)
  {
    if (waypointNodes == null || nodeIndex >= waypointNodes.Length) return 0;
    
    var node = waypointNodes[nodeIndex];
    if (!node.IsBranch) return 0;
    
    // 1. 固定された分岐選択をチェック
    if (fixedBranchChoices.ContainsKey(nodeIndex))
    {
      return fixedBranchChoices[nodeIndex];
    }
    
    // 2. Inspector設定をチェック
    if (manualBranchChoices != null)
    {
      foreach (var choice in manualBranchChoices)
      {
        if (choice.nodeIndex == nodeIndex)
        {
          return Mathf.Clamp(choice.branchIndex, 0, 3);
        }
      }
    }
    
    // 3. デフォルトは0番
    return 0;
  }
  
  
  // 分岐を更新してスプラインを再生成
  public void UpdateBranches()
  {
    GenerateSpline();
    
    // 実行時の場合、現在位置を維持
    if (Application.isPlaying && isMoving)
    {
      PreserveCurrentPosition();
    }
  }
  
  // 現在の位置を新しいスプライン上で維持
  void PreserveCurrentPosition()
  {
    if (splinePoints == null || splinePoints.Length == 0) return;
    
    Vector3 currentPos = transform.position;
    
    // 新しいスプライン上で最も近い位置を見つける
    float closestDistance = float.MaxValue;
    float closestTime = 0f;
    
    // スプライン上の複数点をチェック
    for (int i = 0; i < 100; i++)
    {
      float testTime = i / 99f;
      Vector3 splinePos = GetSplinePosition(testTime);
      float distance = Vector3.Distance(currentPos, splinePos);
      
      if (distance < closestDistance)
      {
        closestDistance = distance;
        closestTime = testTime;
      }
    }
    
    // 現在時間を調整して位置を維持
    currentTime = closestTime * totalTravelTime;
    
    if (enableDebugMode)
    {
    }
  }
  
  // 特定の分岐選択を強制設定
  public void SetBranchChoice(int nodeIndex, int branchIndex)
  {
    if (waypointNodes == null || nodeIndex >= waypointNodes.Length) return;
    if (!waypointNodes[nodeIndex].IsBranch) return;
    
    UpdateBranches();  // 即座にスプライン再生成
  }
  
  // テスト用: ランダム分岐選択を設定（削除予定）
  [ContextMenu("ランダム分岐テスト")]
  public void SetRandomBranchSelection()
  {
    UpdateBranches();
  }
  
  // Inspector用: 分岐設定を生成
  [ContextMenu("分岐設定を自動生成")]
  public void GenerateBranchSettings()
  {
    if (waypointNodes == null) 
    {
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
  }
  
  // Inspector用: 分岐情報を表示
  [ContextMenu("分岐情報を表示")]
  public void ShowBranchInfo()
  {
    if (waypointNodes == null) return;
    
    for (int i = 0; i < waypointNodes.Length; i++)
    {
      var node = waypointNodes[i];
      if (node.IsBranch)
      {
        int selected = GetBranchChoice(i);
        
        for (int j = 0; j < node.branches.Length; j++)
        {
          string marker = j == selected ? "★" : "　";
        }
      }
    }
  }
  
  // キーボード入力処理
  void HandleKeyboardInput()
  {
    if (waypointNodes == null) return;
    
    // 0-3キーで次の分岐選択を指定
    if (Input.GetKeyDown(KeyCode.Alpha0)) SetNextBranchChoice(0);
    if (Input.GetKeyDown(KeyCode.Alpha1)) SetNextBranchChoice(1);
    if (Input.GetKeyDown(KeyCode.Alpha2)) SetNextBranchChoice(2);
    if (Input.GetKeyDown(KeyCode.Alpha3)) SetNextBranchChoice(3);
  }
  
  // デバッグ情報を画面表示
  void OnGUI()
  {
    if (!enableDebugMode || !Application.isPlaying) return;
    
    GUILayout.BeginArea(new Rect(10, 10, 500, 700));
    
    var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
    var headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
    var normalStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
    
    GUILayout.Label("=== 分岐システム デバッグ情報 ===", titleStyle);
    
    // 基本情報
    GUILayout.Label($"ウェイポイント数: {(waypointNodes?.Length ?? 0)}", normalStyle);
    GUILayout.Label($"現在位置: {EstimateCurrentWaypointIndex()}", normalStyle);
    GUILayout.Label($"現在時間: {currentTime:F2} / {totalTravelTime:F2}", normalStyle);
    
    GUILayout.Space(15);
    GUILayout.Label("=== 分岐情報 ===", headerStyle);
    
    // 分岐点情報
    if (waypointNodes != null)
    {
      for (int i = 0; i < waypointNodes.Length; i++)
      {
        var node = waypointNodes[i];
        if (node.IsBranch)
        {
          int selectedBranch = GetBranchChoice(i);
          int validBranches = node.GetValidBranchCount();
          
          GUILayout.Label($"ノード[{i}]: {validBranches}分岐 → 選択中: {selectedBranch}", normalStyle);
          
          // 各分岐の詳細
          for (int j = 0; j < 4; j++)
          {
            if (node.branches[j] != null)
            {
              string marker = j == selectedBranch ? "★" : "　";
              GUILayout.Label($"  {marker}[{j}] {node.branches[j].name}", normalStyle);
            }
          }
          GUILayout.Space(8);
        }
      }
    }
    
    // 外部制御状態
    GUILayout.Space(15);
    GUILayout.Label("=== 制御状態 ===", headerStyle);
    GUILayout.Label($"デバッグモード: 有効", normalStyle);
    
    // 固定された分岐設定
    GUILayout.Label("固定分岐設定:", normalStyle);
    foreach (var kvp in fixedBranchChoices)
    {
      GUILayout.Label($"  ノード{kvp.Key} → 分岐{kvp.Value}", normalStyle);
    }
    
    GUILayout.EndArea();
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

  [Header("デバッグ・表示")]
  public bool enableDebugMode = true;

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
    }

    if (autoStart)
    {
      StartMovement();
    }
  }

  void Update()
  {
    // キーボード制御チェック
    if (enableDebugMode && Application.isPlaying)
    {
      HandleKeyboardInput();
    }
    
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

    if (enableDebugMode && Time.frameCount % 60 == 0)
    {
      float progress = normalizedTime * 100f;
      string mode = useConstantSpeed ? "一定速度" : "時間ベース";
    }
  }

  void GenerateSpline()
  {
    if (waypointNodes == null || waypointNodes.Length < 2)
    {
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

    if (enableDebugMode)
    {
      string loopStatus = loop ? "ループあり" : "ループなし";
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
    if (!node.IsBranch)
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

    if (enableDebugMode)
    {
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
      if (Application.isPlaying == false && enableDebugMode)
      {
        for (int i = 0; i < Mathf.Min(waypointNodes.Length, 5); i++)
        {
          var node = waypointNodes[i];
          string branchInfo = node.IsBranch ? $" ({node.branches.Length}分岐)" : "";
        }
        if (waypointNodes.Length > 5)
        {
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
      return false;
    }

    for (int i = 0; i < waypointNodes.Length; i++)
    {
      var node = waypointNodes[i];
      if (node.mainPoint == null)
      {
        return false;
      }
      
      // 分岐の検証
      if (node.IsBranch)
      {
        for (int j = 0; j < node.branches.Length; j++)
        {
          if (node.branches[j] == null)
          {
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
    }
    else
    {
    }
  }

  void OnDrawGizmos()
  {
    if (!enableDebugMode) return;

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
        if (node.IsBranch)
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
      if (node.IsBranch)
      {
        DrawBranchPoints(i, node);
      }
    }
  }
  
  // 分岐点とその接続を描画
  void DrawBranchPoints(int nodeIndex, WaypointNode node)
  {
    if (node.branches == null) return;
    
    Vector3 mainPos = node.mainPoint.position;
    int selectedBranch = GetBranchChoice(nodeIndex);

    for (int j = 0; j < node.branches.Length; j++)
    {
      if (node.branches[j] == null) continue;
      
      Vector3 branchPos = node.branches[j].position;
      
      // 選択中/未選択で色分け
      if (j == selectedBranch)
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
      // 接続線を描画
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
    // スプライン線を描画

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

    // スプライン点の表示
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