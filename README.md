# ChroMapper-UIPluginDesigner 取扱説明書

**ChroMapper-UIPluginDesigner** は、ChroMapper用プラグインのUI（Menuパネル）を視覚的にデザインし、C#コードまたはJSONレイアウトとして出力するためのツールです。

## 主な機能

*   **UI配置**: ボタン、ラベル、テキスト入力、ドロップダウン、チェックボックスをドラッグ＆ドロップで配置・移動できます。
*   **Layout Group**: **Vertical Layout Group** (縦並び) や **Horizontal Layout Group** (横並び) を作成し、要素を自動的に整列させることができます。グループの中にグループを入れる（入れ子）ことも可能です。
*   **階層構造の編集**:
    *   **ドラッグ＆ドロップ**: 要素をLayout Groupの上にドラッグ＆ドロップすることで、そのグループの子要素として配置できます。
    *   **階層一覧**: インスペクタのドロップダウンリストから、UIの全階層を確認し、直接要素を選択することができます。
    *   **パス表示**: 現在選択している要素の親階層（例: `Root > MainGroup > Header`）をラベルで確認できます。
*   **プロパティ編集**: 各要素のサイズ、テキスト、フォントサイズ、アンカー位置などをインスペクタで編集し、リアルタイムにプレビューできます。
*   **コード出力 (Export Code)**: 作成したレイアウト（階層構造含む）を `HelperUI` クラスを利用したC#コードとして出力します。
*   **JSON保存/読込**: レイアウト情報をJSONファイルとして保存し、後で再編集したり、プラグインのリソースとして利用したりできます。

---

## 操作パネルの解説

画面右側の操作パネルを使ってUIを構築します。

### 1. 要素の追加 (Add Elements)
パネル上部のボタン群（`+ Button`, `+ Label` 等）を押すと、現在選択されている階層（通常はルート、または選択中のLayout Group）の中に新しい要素が追加されます。

### 2. 階層ナビゲーション (Hierarchy)
*   **ドロップダウンリスト**: 現在のUI内の全要素が階層順に表示されます。ここから編集したい要素を選択できます。
*   **パス表示**: ドロップダウンの下に、現在選択中の要素がどの親グループに属しているか（例: `Root > VerticalLayout > Button`）が表示されます。

### 3. ファイル操作
*   **Save**: 作成したレイアウトをJSONファイルとして保存します。
*   **Load**: JSONファイルを読み込んで、エディタ上にレイアウトを復元します。
*   **Export Code**: C#のコード生成機能を使用します。
*   **Close**: デザイナー画面を閉じます。

### 4. メニュー全体設定 (Menu Settings)
操作パネルの中段にある入力欄で、作成するメニューパネル自体の設定を行います。
*   **Menu Size (W, H)**: パネルの幅と高さ。
*   **Menu Pos (X, Y)**: パネルの表示位置。
*   **Menu Anchor (X, Y)**: 画面上の基準位置（0=端, 0.5=中央, 1=端）。

### 5. インスペクタ (Inspector)
画面下部では、現在選択している要素のプロパティを編集できます。

*   **編集コマンド**:
    *   **DEL**: 要素を削除します。
    *   **COPY**: 要素を複製します。
*   **共通プロパティ**:
    *   **Name**: ゲームオブジェクト名。検索やコードからの参照に使用します。（すべての要素で有効）
    *   **Text**: 表示テキスト。（Layout Group選択時は非表示）
    *   **Pos (X, Y)**: 親要素の中心からの相対座標。**※Layout Group配下の要素は自動制御されるため編集不可。**
    *   **Size (W, H)**: 幅と高さ。**※親の Control Child Size が有効な場合は編集不可。**
    *   **Font Size**: 文字サイズ。（Layout Group選択時は非表示）
*   **Layout Group設定** (Vertical/Horizontal Layout選択時のみ有効):
    *   **Pad (L, R, T, B)**: パディング（内側の余白）。
    *   **Spacing**: 要素間のスペース。
    *   **Child Alignment**: 子要素の整列方向（UpperLeft, MiddleCenter等）。
    *   **Control Child Size**: 子要素のサイズをLayout Groupが制御するかどうか。
    *   **Force Expand**: 子要素をエリアいっぱいに広げるかどうか。

---

## 1. Export Code 機能の使い方

デザインしたUIをC#のコードとして出力し、自分のプラグインに組み込む方法です。

### 手順
1.  ChroMapper-UIPluginDesignerでUIを作成します。
2.  **[Export Code]** ボタンを押します。
3.  `.txt` ファイルが保存されるので、その中身（`CreateUI` メソッド）をコピーします。

### 必要な準備
出力されたコードを動作させるには、ChroMapper-UIPluginDesignerのソースコードに含まれる `HelperUI.cs` があなたのプロジェクトに必要です。

1.  `ChroMapper-UIPluginDesigner` のソースから **`HelperUI.cs`** をあなたのプロジェクトにコピーします。
2.  以下のテンプレートを使ってプラグインのUIクラスを作成します。

### 実装テンプレート (C#)

```csharp
using UnityEngine;
using UnityEngine.UI; // LayoutGroup等のために必要
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
            // 自動生成されたコードの例:
            // ui.AddButton(menu.transform, "Button0", ...);
            
            // Layout Groupの例:
            // var group = new GameObject("VLayout");
            // group.transform.SetParent(menu.transform, false);
            // ...
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
*   **`UIConstants.cs`** (キー定数定義)

※ `UIConstants.cs` はJSONのキー定義を含むため、すべてコピーするか、必要な部分を抽出して利用してください。

#### 参照の追加
*   **`Plugins.dll`**: ChroMapperのインストールフォルダに含まれる `Plugins.dll` を参照に追加してください（`SimpleJSON` を利用するため）。

### 実装テンプレート (C#)

```csharp
using UnityEngine;
using System.IO;
using SimpleJSON;
using System.Reflection; // Assembly利用に必要
using System.Collections.Generic; // List利用に必要

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
            float w = root[UIConstants.KeyPanelWidth].AsFloat;
            float h = root[UIConstants.KeyPanelHeight].AsFloat;
            float ax = root[UIConstants.KeyPanelAnchorX] != null ? root[UIConstants.KeyPanelAnchorX].AsFloat : 0.5f;
            float ay = root[UIConstants.KeyPanelAnchorY] != null ? root[UIConstants.KeyPanelAnchorY].AsFloat : 0.5f;
            float px = root[UIConstants.KeyPanelPosX].AsFloat;
            float py = root[UIConstants.KeyPanelPosY].AsFloat;

            _ui.MoveTransform(_menuPanel.transform, w, h, ax, ay, px, py);

            // 3. UILayoutBuilderを使って中身の要素を一括生成
            // (Layout Groupや階層構造も自動的に構築されます)
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
        }
    }
}
```

## ヒント
*   **階層の操作**: 要素を Layout Group の中に移動するには、要素をドラッグして目的の Layout Group の上でドロップしてください。グレーの背景部分にドロップすると、要素は最上位（ルート）階層に移動します。
*   **Layout Group の入れ子**: Layout Group の中に別の Layout Group を入れることも可能です。これにより、複雑なグリッド状のレイアウトなども作成できます。

---

## 3. 保存されるJSONフォーマット仕様

保存されるJSONファイルは以下の構造を持っています。この仕様を理解することで、外部ツールでの編集や動的な生成が可能になります。

### ルートオブジェクト (Root Object)

JSONのルートには、メインとなるメニューパネルの設定と、子要素のリストが含まれます。

| キー (Key) | 型 (Type) | 説明 (Description) |
| :--- | :--- | :--- |
| `PanelWidth` | Number | メニューパネル全体の幅 |
| `PanelHeight` | Number | メニューパネル全体の高さ |
| `PanelAnchorX` | Number | パネルのアンカーX (0.0=左, 0.5=中央, 1.0=右) |
| `PanelAnchorY` | Number | パネルのアンカーY (0.0=下, 0.5=中央, 1.0=上) |
| `PanelPosX` | Number | パネルのX座標位置 (AnchoredPosition) |
| `PanelPosY` | Number | パネルのY座標位置 (AnchoredPosition) |
| `Elements` | Array | パネルに含まれるUI要素オブジェクトのリスト |

### UI要素オブジェクト (Element Object)

`Elements` 配列、および Layout Group の `Children` 配列に含まれるオブジェクトです。

**共通プロパティ:**

| キー (Key) | 型 (Type) | 説明 (Description) |
| :--- | :--- | :--- |
| `Type` | String | 要素の種類。<br>有効な値: `Button`, `Label`, `TextInput`, `Dropdown`, `Checkbox`, `VerticalLayout`, `HorizontalLayout` |
| `Name` | String | 要素の名前 (UnityのGameObject名になります) |
| `Text` | String | 表示テキスト (ボタンのラベルやテキスト入力の初期値など) |
| `AnchorPosX` | Number | アンカーからのX相対位置 |
| `AnchorPosY` | Number | アンカーからのY相対位置 |
| `SizeX` | Number | 幅 |
| `SizeY` | Number | 高さ |
| `FontSize` | Number | フォントサイズ |

**Layout Group (VerticalLayout, HorizontalLayout) 専用プロパティ:**

`Type` が `VerticalLayout` または `HorizontalLayout` の場合、以下のプロパティが有効になります。これらは Unity の Layout Group コンポーネントの設定に対応します。

| キー (Key) | 型 (Type) | 説明 (Description) |
| :--- | :--- | :--- |
| `PaddingTop` | Integer | 上端のパディング |
| `PaddingBottom` | Integer | 下端のパディング |
| `PaddingLeft` | Integer | 左端のパディング |
| `PaddingRight` | Integer | 右端의パディング |
| `Spacing` | Number | 子要素間のスペース |
| `ChildAlignment` | String | 子要素の整列方向 (例: `UpperLeft`, `MiddleCenter`) |
| `ChildControlWidth` | Boolean | 子要素の幅を自動制御するか |
| `ChildControlHeight` | Boolean | 子要素の高さを自動制御するか |
| `ChildForceExpandWidth` | Boolean | 子要素を幅いっぱいに強制的に広げるか |
| `ChildForceExpandHeight` | Boolean | 子要素を高さいっぱいに強制的に広げるか |
| `Children` | Array | Layout Group に含まれる子要素のリスト (入れ子構造が可能) |