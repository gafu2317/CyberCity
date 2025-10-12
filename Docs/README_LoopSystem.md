# SplineRouteCamera 使用ガイド

## 基本設定

- `waypointNodes`: ルート上のポイント（子オブジェクトで分岐作成）
- `totalTravelTime`: 1周にかかる時間
- `isLooping`: ループ有効/無効
- `enableDebugMode`: デバッグ表示の有効/無効

## 分岐システム

### 分岐ポイントの作成
1. ウェイポイントに子オブジェクトを4つ追加
2. 4つすべて設定されると自動で分岐点として認識

### 分岐制御方法

#### スクリプトから制御
```csharp
// カメラコンポーネントを取得
SplineRouteCamera camera = GetComponent<SplineRouteCamera>();

// 次の分岐点で分岐0を選択
camera.SetNextBranchChoice(0);  // 0-3の値

// 例：ランダム分岐
int randomBranch = Random.Range(0, 4);
camera.SetNextBranchChoice(randomBranch);
```

#### キーボードテスト（デバッグモード時）
- `0`キー: 次の分岐で選択肢0を選択
- `1`キー: 次の分岐で選択肢1を選択  
- `2`キー: 次の分岐で選択肢2を選択
- `3`キー: 次の分岐で選択肢3を選択

### 分岐の動作
- `SetNextBranchChoice()`を呼ぶと次に来る分岐点が指定される
- 一度設定すると、その分岐点では常に同じ選択肢を使用
- 設定しない分岐点はデフォルト（0番）を選択

## デバッグ表示

`enableDebugMode = true`で画面左上に表示:
- 現在位置とウェイポイント数
- 各分岐点の状態（★=選択中）
- 固定された分岐設定

## よくある問題

| 問題 | 解決策 |
|------|--------|
| 分岐が変わらない | Inspector設定を確認 |
| ループ時に停止 | `loopSpeedCurve`をLinearに |
| キーボード操作できない | `enableDebugMode = true` |