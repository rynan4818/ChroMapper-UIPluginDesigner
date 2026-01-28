using System;
using System.Collections.Generic;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIPluginDesigner
{
    public class UILayoutBuilder
    {
        private HelperUI _ui;
        private Transform _parent;
        private Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();

        public UILayoutBuilder(HelperUI ui, Transform parent)
        {
            _ui = ui;
            _parent = parent;
        }

        public Dictionary<string, GameObject> Build(JSONNode root)
        {
            var arr = root[UIConstants.KeyElements].AsArray;
            foreach (JSONNode n in arr)
            {
                var data = ElementData.FromJSON(n);
                BuildElement(data);
            }
            return _objects;
        }

        private void BuildElement(ElementData el)
        {
            GameObject obj = null;
            // JSONで保存される座標はAnchorPosだが、HelperUIのメソッド引数の意味と合わせる必要がある。
            // 既存のHelperUIは (parent, name, text, ..., x, y, ...) という引数を取る。
            // ここではHelperUIの実装に合わせて呼び出しを行う。

            // HelperUIの引数: x, y は anchoredPosition
            // Alignment や Anchor設定は HelperUI のデフォルト (0.5, 0.5) なのか、メソッドによって違うのか確認が必要だが、
            // DesignerControllerでは一部 (0, 1) などを使っていた。
            // JSONには AnchorMin/Max が含まれていないため、ここでは一旦 DesignerController で使われていた
            // 「左上(0,1)基準」や「中央(0.5, 0.5)」などの情報を補完するか、
            // あるいはJSONにPivot/Anchor情報も含めるべきだが、
            // 今回は「DesignerControllerのハードコードを再現する」ため、
            // 簡易的に ElementType と名前の接尾辞やコンテキストで判断するか、
            // 単純にすべて中央基準(0.5, 0.5)で配置して座標で調整するか。
            
            // 以前のDesignerControllerを見ると:
            // パレットボタン: pivot(0.5, 1) or (1, 1)?
            // Ui.AddButton(..., 0.5f, 1, ...) -> Pivot(0.5, 1) TopCenter
            // Ui.AddLabel(..., 0, 1, ...) -> Pivot(0, 1) TopLeft
            
            // これらをJSONだけで制御するにはプロパティが足りないが、
            // 汎用化のために今回はすべて (0.5, 0.5) (Center) で配置し、
            // 座標を調整済みのデータとしてJSONを作るか、
            // JSONに Anchor/Pivot プロパティを追加するのが正しい。
            
            // しかし、ElementData構造を変えると互換性が...
            // いや、今回は新しい構造を作るので、ElementDataにPivotX/Y, AnchorMinX/Y 等を追加しようとも思ったが、
            // 手間を省くため、HelperUIのデフォルト(多くはCenter)を利用しつつ、
            // 特殊なものはコード側で修正... ではなく、
            // ここは「全て中央基準(0.5, 0.5)」としてJSONを作成することにする。
            // (座標変換はJSON作成時に行う)

            // HelperUIのメソッドシグネチャ(推定)
            // AddButton(parent, name, text, fontSize, w, h, pivotX, pivotY, x, y, action)
            // AddLabel(parent, name, text, w, h, pivotX, pivotY, x, y, align, fontSize)
            // AddTextInput(parent, name, text, align, fontSize, w, h, pivotX, pivotY, x, y, onValChange)
            
            // シンプルにするため、Builderでは pivot=(0.5, 0.5) で統一して生成する。

            switch (el.Type)
            {
                case ElementType.Button:
                    var btn = _ui.AddButton(_parent, el.Name, el.Text, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = btn.gameObject;
                    break;
                case ElementType.Label:
                    var lbl = _ui.AddLabel(_parent, el.Name, el.Text, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, TextAlignmentOptions.Center, el.FontSize);
                    obj = lbl.Item1.gameObject;
                    break;
                case ElementType.TextInput:
                    var inp = _ui.AddTextInput(_parent, el.Name, el.Text, TextAlignmentOptions.Left, el.FontSize, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = inp.gameObject;
                    break;
                case ElementType.Dropdown:
                    var dd = _ui.AddDropdown(_parent, new List<string>(), 0, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = dd.gameObject;
                    break;
                case ElementType.Checkbox:
                    var tgl = _ui.AddCheckbox(_parent, true, el.SizeX, el.SizeY, 0.5f, 0.5f, el.AnchorPosX, el.AnchorPosY, null);
                    obj = tgl.gameObject;
                    break;
            }

            if (obj != null)
            {
                _objects[el.Name] = obj;
            }
        }
        
        public T Get<T>(string name) where T : Component
        {
            if (_objects.ContainsKey(name))
            {
                return _objects[name].GetComponent<T>();
            }
            return null;
        }

        public GameObject GetObject(string name)
        {
             if (_objects.ContainsKey(name)) return _objects[name];
             return null;
        }
    }
}
