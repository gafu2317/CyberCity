# SplineRouteCamera ループシステム

## 概要

カメラがスプライン経路を継続的に周回するシステム。複数のメソッドが連携してスムーズなループを実現する。

## 処理の流れ

### 1. 初期化 (Start)
**`Start()`** → **`GenerateSpline()`** → **`PrepareControlPoints()`**

- ウェイポイントからスプラインポイント配列を生成
- ループ時は最初と最後を繋ぐ制御点を追加
- カメラを開始位置に配置

### 2. フレーム毎の処理 (Update)

#### ステップ1: 時間管理
- `currentTime`を継続的に増加
- `normalizedTime = currentTime / totalTravelTime`で0-1に正規化
- ループ時は`normalizedTime % 1f`で0-1範囲を循環

#### ステップ2: スピードカーブ適用
- **非ループ**: `speedCurve`を使用
- **ループ**: `loopSpeedCurve`を使用（停止防止のため）
- カーブで時間を変換して自然な加減速を実現

#### ステップ3: 位置計算
**2つの方法から選択:**
- **時間ベース**: `GetSplinePosition()`でスプライン補間
- **距離ベース**: `GetSplinePositionByDistance()`で一定速度移動

#### ステップ4: カメラ更新
- 計算された位置にカメラを移動
- `HandleSplineRotation()`で向きを調整

## 各メソッドの役割

### `PrepareControlPoints()`
**ループ用制御点配列を生成**
- 通常: [w0, w1, w2, ..., wN]
- ループ: [last, w0, w1, ..., wN, w0, w1]
- 最後→最初への滑らかな繋ぎを実現

### `GetSplinePosition(float time)`
**時間から位置を計算**
- ループ時: modulo演算で配列インデックスを循環
- 線形補間で滑らかな移動を実現

### `GetSplinePositionByDistance(float distance)`
**距離から位置を計算**
- 一定速度での移動に使用
- スプライン全長に対する距離比で位置を決定

## パラメータ

### 基本設定
- `loop`: ループの有効/無効
- `totalTravelTime`: 1周にかかる時間（秒）

### スピード制御
- `speedCurve`: 通常時の速度変化
- `useLoopSpeedCurve`: ループ専用カーブを使用するか
- `loopSpeedCurve`: ループ時専用の速度カーブ

## ループ停止問題の解決

### 問題
EaseInOutカーブは開始時（t=0）で速度が0になるため、ループ時に一時停止が発生。

### 解決策
1. `useLoopSpeedCurve = true`に設定
2. `loopSpeedCurve`をLinearカーブに設定
3. これにより0の位置でも速度が維持される

## 推奨設定

### スムーズなループ
```
loop = true
useLoopSpeedCurve = true  
loopSpeedCurve = Linear(0,0,1,1)
```

### 一回通過
```
loop = false
speedCurve = EaseInOut(0,0,1,1)
```

## よくある問題

| 問題 | 原因 | 解決策 |
|------|------|--------|
| ループ時に停止 | EaseInOutカーブ | useLoopSpeedCurve=true |
| カクカクした動き | 解像度不足 | splineResolution を100以上 |
| スプライン生成エラー | ウェイポイント不足 | 最低3個のウェイポイント |
| ループが不自然 | 配置が悪い | 最初と最後のポイントを近づける |