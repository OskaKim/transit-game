# Transit Game Prototype — 設計書

Mini Metro を参考にした交通系ゲームのプロトタイプ。
元計画書: `TRANSIT_GAME_PLAN.md`(Downloads)。本書は **実装後の実態** を反映した設計図である。

---

## 1. 最重要方針: コアと Unity の分離

シミュレーションのコアロジックは **UnityEngine に依存しない純粋な C#** で実装している。
依存禁止は規約ではなく **asmdef の `noEngineReferences: true` でコンパイラレベルで強制** している。

```
┌─────────────────────────────┐         ┌─────────────────────────────┐
│  TransitCore (Pure C#)      │ events  │  TransitGame.Unity          │
│  Assets/.../Scripts/Core    │ ──────► │  Assets/.../Scripts/Unity   │
│                             │         │                             │
│  - グラフ構造・経路探索        │ ◄────── │  - 描画 (Mesh/LineRenderer) │
│  - 乗客スポーン・乗降ロジック   │ method  │  - 入力 (Input System)      │
│  - 車両移動 (抽象座標)        │  calls  │  - HUD (uGUI, コード生成)    │
│  - ゲームルール・スコア        │         │  - カメラ設定               │
└─────────────────────────────┘         └─────────────────────────────┘
```

接続ルール:

- **Unity → コア**: メソッド呼び出しのみ(`engine.Tick(dt)`, `engine.TryCreateLine(a, b, out id)` など)
- **コア → Unity**: C# イベントのみ(`StationSpawned`, `LineChanged`, `GameOverTriggered` など)
- コアの座標は `System.Numerics.Vector2`。Unity 側で `UnityEngine.Vector3` に変換
- コアの乱数は `System.Random`(シード指定可)。同シードで同展開を再現できる
- 連続値(列車位置・待機列・混雑タイマー)はイベントではなく **Unity 側が毎フレームポーリング** する
  (イベント配線を最小に保つための意図的な設計。生成/削除のみイベント駆動)

## 2. フォルダ構成

```
Assets/TransitGame/
  README.md                    # 入口。本書への案内
  Documentation~/
    ARCHITECTURE.md            # 本書 (~付きフォルダはUnity非インポート)
  Config/
    GameConfig.asset           # チューニングパラメータ (ScriptableObject)
  Scenes/
    TransitGame.unity          # ゲームシーン (Camera + Bootstrap のみ)
  Scripts/
    Core/                      # ★ UnityEngine 参照禁止ゾーン
      TransitCore.asmdef       #   noEngineReferences: true
      Model/
        StationShape.cs        #   enum: Circle / Triangle / Square
        Station.cs             #   駅: Id, Shape, Position, 待機列, 混雑タイマー
        Line.cs                #   路線: 駅IDの順序リスト, 色Index
        Train.cs               #   車両: 所属路線, 区間(From/To index)+進行度, 乗客
        Passenger.cs           #   乗客: 目的Shape (経路は保持しない→§4)
        TransitNetwork.cs      #   駅・路線の集合, 隣接取得, Version管理
      Simulation/
        SimConfig.cs           #   パラメータの素のC#クラス
        SimulationEngine.cs    #   Tick駆動の中心。公開API・イベントの窓口
        PassengerRouter.cs     #   経路探索 (Dijkstra + キャッシュ)
        SpawnSystem.cs         #   駅・乗客のスポーンタイマー制御
        GameRules.cs           #   混雑判定 → ゲームオーバー
    Unity/                     # Unity依存ゾーン
      TransitGame.Unity.asmdef #   refs: TransitCore, Unity.InputSystem
      Bootstrap.cs             #   エントリポイント。コア生成・Tick駆動・ビュー管理
      Config/GameConfig.cs     #   ScriptableObject → SimConfig 変換
      View/
        VisualFactory.cs       #   手続き生成メッシュ・URP Unlitマテリアル
        StationView.cs         #   駅描画・待機客アイコン・混雑の赤化
        LineView.cs            #   LineRenderer による路線描画
        TrainView.cs           #   車両の移動表示・乗客数ラベル
      Input/
        LineEditController.cs  #   ドラッグ路線編集 (新Input System)
      UI/
        HUDController.cs       #   スコア/在庫/時間・ゲームオーバー画面 (全てコード生成)
  Tests/
    EditMode/
      TransitGame.Tests.EditMode.asmdef
      CoreSimulationTests.cs   #   コアのみのユニットテスト6本
```

## 3. クラス責務とデータフロー

### SimulationEngine(コアの窓口)

外部(Unity・テスト)が触るのは原則このクラスだけ。

| 公開API | 役割 |
|---|---|
| `Tick(float dt)` | 全状態を dt 秒進める(スポーン→列車→ルール判定の順) |
| `Initialize()` | 初期駅(○△□を1つずつ)を配置 |
| `TryCreateLine(a, b, out id)` | 新規路線+列車1編成を生成(在庫チェック込み) |
| `TryExtendLine(lineId, end, new)` | 路線の端を延長(先頭挿入時は列車indexを補正。環状線は不可) |
| `TryInsertStation(lineId, segIdx, st)` | 既存路線の区間に中間駅を挿入 |
| `TryCloseLoop(lineId)` | 3駅以上の路線を環状化(列車は反転せず周回するようになる) |
| `TryAddTrain(lineId)` | 路線に列車を増結 |
| `TryRemoveLine(lineId)` | 路線削除。乗車中の客は最寄り駅に降ろす |
| `GetTrainPosition / GetTrainSegment` | ビュー用の補間座標取得(ポーリング用) |
| `AddStationAt(shape, pos)` | テスト・デバッグ用の駅直接配置 |

| イベント | 発火タイミング |
|---|---|
| `StationSpawned(Station)` | 駅生成(初期配置含む) |
| `LineChanged(Line)` | 路線の新規作成・延長 |
| `LineRemoved(int)` / `TrainAdded(Train)` / `TrainRemoved(int)` | 各生成・削除 |
| `ScoreChanged(int)` | 乗客を目的地に運んだとき |
| `GameOverTriggered()` | 混雑タイマー満了 |

### 1 Tick の処理順

```
Tick(dt)
 ├─ SpawnSystem.Tick      … 駅スポーン(間隔は漸減)・駅ごとの乗客スポーン
 ├─ TickTrain × 全列車    … dwell消化 → 区間進行 → 駅到着時に乗降処理 (§4)
 └─ GameRules.Tick        … 待機列 > 上限 の駅の混雑タイマーを加算
                            (上限以下なら2倍速で回復)。猶予超過で GameOver
```

### Unity 側の流れ

```
Bootstrap.Start
 ├─ カメラ設定 / HUDController / LineEditController を自身に追加
 └─ StartGame()
     ├─ GameConfig.ToSimConfig() + シード決定 (useRandomSeed なら TickCount)
     ├─ new SimulationEngine() → イベント購読 → Initialize()
     └─ ビュー辞書 (stationId/lineId/trainId → View) を World 親の下に構築

Bootstrap.Update → engine.Tick(Time.deltaTime)
各View.Update/LateUpdate → コア状態をポーリングして表示更新
リスタート → World ごと破棄して StartGame() し直す(エンジンは使い捨て)
```

ビュー・HUD・EventSystem は **全て実行時にコードで生成** する。
シーンには Camera と Bootstrap(+GameConfig参照)しか置かない。
プレハブ管理が不要になり、シーンのマージ衝突も起きないための選択である。

## 4. 乗客の経路探索(本プロトタイプの肝)

### アルゴリズム

`PassengerRouter` は **(駅, 乗車中路線) を状態とする Dijkstra**:

- エッジ = 路線上の隣接駅間。コスト 1
- 路線を乗り換えるとき +`TransferPenalty`(初期値 2 = 計画書の「乗り換え1回 = +2ホップ相当」)
- ゴール = 目的 Shape を持つ任意の駅
- 戻り値は経路全体ではなく **最初の一手 `RouteStep(lineId, nextStationId)` のみ**

### 計画書からの意図的な変更点

計画書は「乗客が計算済み経路を保持する」想定だったが、実装では
**乗客は経路を持たず、駅に停まるたびに次の一手を再計算する** 方式にした。

- 乗車判定: 待機客は「列車の次の停車駅 == 自分の最善の次駅」なら乗る
- 降車判定: 到着駅が目的Shapeなら降車(スコア+1)。
  そうでなければ次の一手を再計算し、この列車が合わなければ降りて待機列へ(=乗り換え)
- 路線を引き直しても乗客の状態修復が不要(次の停車で自然に再計算される)。
  Phase 2 の「路線変更後も破綻なく動く」要件がこれで自動的に満たされる

### キャッシュ

`(stationId, targetShape) → RouteStep?` を辞書キャッシュ。
`TransitNetwork.Version`(路線・駅の変更で増加)が変わったら全クリア。イベント駆動の無効化に相当。

## 5. 車両移動モデル

- 位置 = `lerp(line.Stations[FromIndex], line.Stations[ToIndex], Progress)` の抽象座標
- 駅到着で `DwellTime` 停車 → 乗降処理 → 終端なら方向反転
- 環状線(`Line.IsLoop`)では反転せず index を modulo で回して周回する
- 1路線に複数列車を走らせられる(`TryAddTrain`)
- 路線延長で先頭に駅が挿入された場合は index を +1 補正
- index が路線と矛盾したら(路線編集後)最寄りの正常な区間にスナップ(`ClampTrainToLine`)
- 路線削除時は列車も削除し、乗客は出発駅の待機列に放出

## 6. パラメータ(GameConfig.asset で調整)

| パラメータ | 初期値 | 備考 |
|---|---|---|
| 初期駅数 | 3 | ○△□を1つずつ |
| 駅スポーン間隔 | 20秒 → 最短8秒 | 毎回 ×0.92 で漸減(難易度カーブ) |
| 乗客スポーン間隔 | 駅ごと 3〜6秒 | 一様乱数 |
| 駅の待機上限 | 6人 | 超過で混雑タイマー加算 |
| 混雑猶予 | 8秒 | 満了でゲームオーバー |
| 車両速度 / 定員 | 2.0 units/s / 6人 | |
| 路線数上限 | 3 | 1路線につき列車1編成(固定) |
| 乗り換えペナルティ | 2 | 経路探索コスト |
| ワールドサイズ | 14×8 units | 駅間最低距離 2.2 |

## 7. テスト

`Tests/EditMode/CoreSimulationTests.cs` — **TransitCore のみ参照**(Unityシーン不要):

1. 単一路線で乗客が配達される
2. 乗り換えが必要な経路を Router が見つける
3. 経路がなければ null
4. 路線追加でキャッシュが無効化される
5. 2路線+乗り換えで実際に配達される
6. 中間駅挿入後も配達される(重複挿入は拒否)
7. 環状線の列車は反転せず周回する(延長は拒否)
8. 同一路線に2列車が破綻なく走る
9. 混雑でゲームオーバーが発火する

Unity Test Runner (EditMode) で実行。

## 8. 操作仕様

ドラッグは Mini Metro 同様の **経路積み上げ方式**(`LineEditController`):

| 操作 | 挙動 |
|---|---|
| 駅から左ドラッグ開始 | 経路の起点を掴む |
| ドラッグ中に別の駅へ触れる | 経由駅として経路に追加(プレビュー線が伸びる) |
| 直前の駅へ戻る | 最後の一手を取消 |
| 経路上の他の駅へ触れる | その駅を経路から除外(止まらなくなる) |
| 離す | 経路全体を一括コミット。隣接ペアごとに 延長 → 新規 → 環状化 を自動判定。<br>既に同区間がある場合はスキップ(重複路線を作らない) |
| 駅1つだけ掴んで路線上で離す | その区間に中間駅として挿入 |
| 路線上で右クリック | 路線削除(在庫が戻る) |
| 右上のデバッグバー | x1/x2/x4 タイムスケール、+列車(最も列車が少ない路線に増結) |
| ゲームオーバー画面の「もう一度」 | World破棄 → エンジン再生成 |

### 路線のレーン分け描画

同じ駅間を複数路線が通る区間は、`LineLaneLayout` が路線ごとに垂直方向の
レーンオフセット(0.24 units 間隔)を割り当てて並走表示する。

- 無向エッジ `(駅id小, 駅id大)` → そのエッジを使う路線idリスト、の辞書を路線変更のたびに再構築
- 頂点のオフセット = 隣接2区間のオフセットの平均(角でも線が繋がる)
- 列車も自路線のレーン上を走る(`TrainView` が同じオフセットを参照)
- 純粋に表示の問題なので **コアは関知しない**(Unity層の `LineLaneLayout` に閉じる)

入力は新 Input System(`Mouse.current`)のみ。プロジェクト設定が
`activeInputHandler: 1`(新方式のみ)のため legacy `Input` は使えない点に注意。

## 9. 未実装(Phase 4 / スコープ外)

- 週次報酬(車両追加 or 新路線の二択。コア側の `TryAddTrain` は実装済みなので、UI演出のみ)
- 路線描画の45度スナップ整形
- セーブ/ロード、川とトンネル、モバイル対応

## 10. 他ゲームへの流用手順

1. `Scripts/Core/` を丸ごと新プロジェクトへコピー(asmdef ごと)
2. `Station` → 停留所/倉庫、`StationShape` → 品目などへ意味を読み替え
   (描画の意味付けは全て Unity 層にあるのでコアの変更は最小)
3. `SimulationEngine` の公開APIとイベントに対して新しい Presentation 層を書く
4. ロジック変更は EditMode テストで Unity 起動なしに検証できる
