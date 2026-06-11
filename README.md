# transit-game

Unity 6 (URP) のサンドボックスプロジェクト。主な内容は2つ:

## 1. Transit Game Prototype

Mini Metro を参考にした交通系ゲームのプロトタイプ。
コアロジック(UnityEngine 非依存の純粋C#)と Unity 表示層を分離した、
交通系ゲーム開発の再利用可能な土台。

- 入口: [Assets/TransitGame/README.md](Assets/TransitGame/README.md)
- 設計書: [Assets/TransitGame/Documentation~/ARCHITECTURE.md](Assets/TransitGame/Documentation~/ARCHITECTURE.md)
- シーン: `Assets/TransitGame/Scenes/TransitGame.unity`

## 2. Unity-chan Toon Shader 実験

`Assets/MCUnitychan/` 配下のユニティちゃんモデルと
Unity Toon Shader を使った実験用アセット。

## 環境

- Unity 6000.3.6f1
- Universal Render Pipeline (URP) 17.x
- Input System (新方式のみ)

## ライセンス表記

本リポジトリにはユニティちゃんアセットが含まれます。

このアセットは、『ユニティちゃんライセンス条項』に基づいて公開・配布されるものです。
本アセットをご利用される場合は、『キャラクター利用のガイドライン』をお守りいただく必要があります。

- ユニティちゃんライセンス: https://unity-chan.com/contents/license_jp/
- ライセンス全文は [Assets/MCUnitychan/License/UCL2.0/](Assets/MCUnitychan/License/UCL2.0/) に同梱

© Unity Technologies Japan/UCL

Transit Game 部分(`Assets/TransitGame/` 配下)のソースコードは自作です。
