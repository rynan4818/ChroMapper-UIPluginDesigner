# ChroMapper-UIPluginDesigner 取扱説明書

**ChroMapper-UIPluginDesigner** は、ChroMapper用プラグインのUI（Menuパネル）を視覚的にデザインし、C#コードまたはJSONレイアウトとして出力するためのツールです。

## 主な機能

*   **UI配置**: ボタン、ラベル、テキスト入力、ドロップダウン、チェックボックスをドラッグ＆ドロップで配置・移動できます。
*   **プロパティ編集**: 各要素のサイズ、テキスト、フォントサイズ、アンカー位置などをインスペクタで編集し、リアルタイムにプレビューできます。
*   **コード出力 (Export Code)**: 作成したレイアウトを `HelperUI` クラスを利用したC#コードとして出力します。
*   **JSON保存/読込**: レイアウト情報をJSONファイルとして保存し、後で再編集したり、プラグインのリソースとして利用したりできます。

---

## 1. Export Code 機能の使い方

デザインしたUIをC#のコードとして出力し、自分のプラグインに組み込む方法です。

### 手順
1.  ChroMapper-UIPluginDesignerでUIを作成します。
2.  **[Export]** ボタンを押します。
3.  `.txt` ファイルが保存されるので、その中身（`CreateUI` メソッド）をコピーします。

### 必要な準備
出力されたコードを動作させるには、ChroMapper-UIPluginDesignerのソースコードに含まれる `HelperUI.cs` があなたのプロジェクトに必要です。

1.  `ChroMapper-UIPluginDesigner` のソースから **`HelperUI.cs`** をあなたのプロジェクトにコピーします。
2.  以下のテンプレートを使ってプラグインのUIクラスを作成します。

### 実装テンプレート (C#)

```csharp
using UnityEngine;
using TMPro;
using System.Collections.Generic;

// 名前空間は自分のプロジェクトに合わせて変更してください
namespace MyPlugin
{
    public class MyPluginUI : MonoBehaviour
    {
        private HelperUI _ui;
        private GameObject _menuCanvas;

        public void Start()
        {
            // HelperUIの初期化（未アタッチの場合）
            _ui = gameObject.AddComponent<HelperUI>();
            
            // キャンバスの取得（ChroMapperのCanvasを探す）
            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            if (canvas == null) return;

            // メニューパネルの作成
            var menuObj = new GameObject("MyPluginMenu", typeof(RectTransform));
            menuObj.transform.SetParent(canvas.transform, false);
            
            // 背景やサイズの設定 (ChroMapper-UIPluginDesignerで作ったパネル設定に合わせて調整)
            _ui.AttachImage(menuObj, new Color(0.24f, 0.24f, 0.24f, 0.95f));
            _ui.MoveTransform(menuObj.transform, 250, 190, 0.5f, 0.5f, 0, 0); // 幅, 高さ, AnchorX, Y, PosX, Y

            // UI要素の構築
            CreateUI(_ui, menuObj);
        }

        // ▼ここに Export Code で出力された内容を貼り付けます▼
        private void CreateUI(HelperUI ui, GameObject menu)
        {
            // 例:
            ui.AddButton(menu.transform, "Button0", "Click Me", 14, 100, 30, 0.5f, 0.5f, 0, 0, () => {
                Debug.Log("Clicked!");
            });
        }
        // ▲貼り付けここまで▲
    }
}
```

---

## 2. JSONレイアウトを埋め込みリソースとして利用する方法

ChroMapper-UIPluginDesigner自身が行っているように、レイアウトをJSONファイルとして保存し、それをDLLに埋め込んで実行時に読み込む方法です。コードを書き換えることなくUIの配置を変更できるようになります。

### 手順
1.  ChroMapper-UIPluginDesignerでUIを作成し、**[Save]** ボタンでJSONファイル（例: `layout.json`）を保存します。
2.  そのJSONファイルをVisual Studioのプロジェクトに追加します。
3.  追加したJSONファイルのプロパティを開き、**「ビルドアクション」を `埋め込みリソース (Embedded Resource)`** に設定します。

### 必要な準備
JSONを解析してUIを構築するために、以下のファイル（ChroMapper-UIPluginDesignerのソースコード）をあなたのプロジェクトにコピーしてください。

*   **`DataTypes.cs`** (データ構造定義)
*   **`UILayoutBuilder.cs`** (JSONからUIを作るクラス)
*   **`HelperUI.cs`** (UI生成ヘルパー)

#### 参照の追加
*   **`Plugins.dll`**: ChroMapperのインストールフォルダに含まれる `Plugins.dll` を参照に追加してください（`SimpleJSON` を利用するため）。

#### 定数定義クラスの作成
`UIConstants.cs` はすべてコピーするのではなく、JSON読み込みに必要な定数のみを含む以下のクラスをプロジェクト内に作成してください。

```csharp
namespace ChroMapper_UIPluginDesigner // あなたのプロジェクトの名前空間に合わせてください
{
    public static class UIConstants
    {
        // JSON Root Keys
        public const string KeyPanelWidth = "PanelWidth";
        public const string KeyPanelHeight = "PanelHeight";
        public const string KeyPanelAnchorX = "PanelAnchorX";
        public const string KeyPanelAnchorY = "PanelAnchorY";
        public const string KeyPanelPosX = "PanelPosX";
        public const string KeyPanelPosY = "PanelPosY";
        public const string KeyElements = "Elements";

        // Element Property Keys
        public const string KeyType = "Type";
        public const string KeyName = "Name";
        public const string KeyText = "Text";
        public const string KeyAnchorPosX = "AnchorPosX";
        public const string KeyAnchorPosY = "AnchorPosY";
        public const string KeySizeX = "SizeX";
        public const string KeySizeY = "SizeY";
        public const string KeyFontSize = "FontSize";
    }
}
```

### 実装テンプレート (C#)

```csharp
using UnityEngine;
using System.IO;
using SimpleJSON;
using System.Reflection; // Assembly利用に必要

namespace MyPlugin
{
    public class JsonBasedUI : MonoBehaviour
    {
        private HelperUI _ui;
        private GameObject _menuPanel;

        public void Start()
        {
            _ui = gameObject.AddComponent<HelperUI>();
            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            if (canvas == null) return;

            // 1. 埋め込みリソースからJSONを読み込む
            // リソース名は "デフォルト名前空間.フォルダ名.ファイル名.拡張子" になります
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MyPlugin.Resources.layout.json"; // ※適切に書き換えてください

            string jsonString = null;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        jsonString = reader.ReadToEnd();
                    }
                }
                else
                {
                    Debug.LogError($"Resource not found: {resourceName}");
                    return;
                }
            }

            // 2. JSONをパースしてパネルを作成
            var root = JSON.Parse(jsonString);
            
            _menuPanel = new GameObject("MyPluginMenu", typeof(RectTransform));
            _menuPanel.transform.SetParent(canvas.transform, false);
            
            // 背景設定
            _ui.AttachImage(_menuPanel, new Color(0.24f, 0.24f, 0.24f, 0.95f));

            // パネルのサイズ・位置をJSONから適用
            // (ChroMapper-UIPluginDesignerの保存形式に準拠)
            float w = root[UIConstants.KeyPanelWidth].AsFloat;
            float h = root[UIConstants.KeyPanelHeight].AsFloat;
            float ax = root[UIConstants.KeyPanelAnchorX] != null ? root[UIConstants.KeyPanelAnchorX].AsFloat : 0.5f;
            float ay = root[UIConstants.KeyPanelAnchorY] != null ? root[UIConstants.KeyPanelAnchorY].AsFloat : 0.5f;
            float px = root[UIConstants.KeyPanelPosX].AsFloat;
            float py = root[UIConstants.KeyPanelPosY].AsFloat;

            _ui.MoveTransform(_menuPanel.transform, w, h, ax, ay, px, py);

            // 3. UILayoutBuilderを使って中身の要素を一括生成
            var builder = new UILayoutBuilder(_ui, _menuPanel.transform);
            builder.Build(root);

            // 4. ボタンなどにイベントを登録
            // builder.Get<T>("要素名") で生成されたオブジェクトを取得できます
            
            // 例: "SaveButton" という名前のボタンにクリックイベントを登録
            var saveBtn = builder.Get<UnityEngine.UI.Button>("SaveButton");
            if (saveBtn != null)
            {
                saveBtn.onClick.AddListener(() => {
                    Debug.Log("Save Button Clicked!");
                });
            }

            // 例: "InputName" という名前の入力欄の値を取得
            var inputName = builder.Get<ChroMapper_UIPluginDesigner.UITextInput>("InputName");
            if (inputName != null)
            {
                inputName.InputField.onValueChanged.AddListener((val) => {
                    Debug.Log("Input Changed: " + val);
                });
            }
        }
    }
}
```

### ヒント
*   **リソース名がわからない場合**: `assembly.GetManifestResourceNames()` を `foreach` で回してログ出力すると、正しいリソース名を確認できます。
*   **HelperUI等の名前空間**: コピーしてきたファイルの `namespace ChroMapper_UIPluginDesigner` は、あなたのプロジェクトの名前空間に変更することをお勧めします。