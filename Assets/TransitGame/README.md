# Transit Game Prototype

Mini Metro を参考にした交通系ゲームのプロトタイプ。
コアロジック(UnityEngine 非依存)と Unity 表示層を分離した、交通系ゲーム開発の再利用可能な土台。

## 遊び方

1. `Scenes/TransitGame.unity` を開いて Play
2. **左ドラッグ**(駅 → 駅): 路線を作成。既存路線の端から引けば延長。
   同じ路線の両端同士を結ぶと環状線、路線の上に落とすと中間駅挿入
3. **右クリック**(路線上): 路線を削除
4. 右上のデバッグバー: タイムスケール(x1/x2/x4)と列車の増結
5. 駅の待機客が上限(6人)を超えて8秒続くとゲームオーバー

## 設計図

**→ [Documentation~/ARCHITECTURE.md](Documentation~/ARCHITECTURE.md)**

アーキテクチャ全体、クラス責務、経路探索の仕組み、パラメータ一覧、
他ゲームへの流用手順はすべて上記設計書にまとめてある。

## 構成の概略

| 場所 | 内容 |
|---|---|
| `Scripts/Core/` | 純C#シミュレーションコア(asmdefで UnityEngine 参照を禁止) |
| `Scripts/Unity/` | 描画・入力・HUD(Unity依存層) |
| `Config/GameConfig.asset` | チューニングパラメータ |
| `Tests/EditMode/` | コアのみのユニットテスト |
| `Documentation~/` | 設計書(Unity非インポートフォルダ) |
