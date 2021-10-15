## 🎬 BEditor

![](https://img.shields.io/github/issues/b-editor/BEditor)
![](https://img.shields.io/github/forks/b-editor/BEditor)
![](https://img.shields.io/github/stars/b-editor/BEditor)
![](https://img.shields.io/github/license/b-editor/BEditor)
![](https://img.shields.io/github/downloads/b-editor/BEditor/total)
![](https://img.shields.io/github/v/release/b-editor/BEditor)
![](https://img.shields.io/github/repo-size/b-editor/BEditor)
![](https://github.com/b-editor/BEditor/workflows/Debug%20Build%20&%20Test/badge.svg)
![](https://github.com/b-editor/BEditor/workflows/CodeQL/badge.svg)
[![Discord](https://img.shields.io/discord/868076100511760385.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/Bm3pnVc928)

BEditorはクロスプラットフォームの動画編集ソフトウェアです。
Windows, Linuxで動作を確認しました。

<img src="https://raw.githubusercontent.com/b-editor/BEditor/main/docs/imgs/ScreenShot_1.png" width="700" />

## 📖 Feature

* GPUを使った処理
* カメラ制御
* ダークモード
* クロスプラットフォーム (0.1.0から)
* キーフレーム機能
* プラグイン機能
* [編集データの値を同期](https://beditor.net/Document/how-to-use/data-binding)
* [シーン機能](https://beditor.net/Document/keywords/scene)
* [30種類以上のエフェクト](https://beditor.net/Document/effects/overview)
* 12種類のオブジェクト
    * フレームバッファ, 画像ファイル, 多角形, 角丸四角形, シーン, 図形, テキスト, 動画ファイル, カメラ

## Requirements

Windows以外では [OpenAL](https://www.openal.org/) 
が必要です

## License

* [MIT License](https://github.com/b-editor/BEditor/blob/main/LICENSE)
* 一部のプラグインは __GPL version 2__ です。
* このソフトウェアは、[Apache 2.0 ライセンス](http://www.apache.org/licenses/LICENSE-2.0)で配布されている製作物が含まれています。

## Building BEditor

* .NET 5.0 SDK が必要です
* 以下のコマンドを実行すると `./publish` に出力されます。
```
dotnet restore
dotnet cake --runtime win-x64;linux-x64
```

## Patrons

- [Bony_Chops](https://www.patreon.com/user/creators?u=52944861)
- [Hayashi Tomonari](https://www.patreon.com/user/creators?u=62872137)
