using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChroMapper_UIPluginDesigner
{
    [Plugin("UIPluginDesigner")]
    public class Plugin
    {
        public static HelperUI ui;
        public static DesignerController designerController;
        private ExtensionButton _extensionBtn;

        [Init]
        public void Init()
        {
            // 常駐するHelperUI（実アプリのUIクラス相当）を初期化
            ui = new GameObject("UIPluginDesigner_UI").AddComponent<HelperUI>();
            GameObject.DontDestroyOnLoad(ui.gameObject);

            // 拡張ボタンの登録
            _extensionBtn = new ExtensionButton
            {
                Tooltip = "UIPluginDesigner",
                Click = ToggleDesigner
            };
            // アイコンは省略(null)または適当なSpriteを設定
            ExtensionButtons.AddButton(_extensionBtn);

            SceneManager.sceneLoaded += SceneLoaded;
        }

        [Exit]
        public void Exit()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Mapperシーン以外では何もしない (BuildIndex 3)
            if (scene.buildIndex != 3) return;

            // コントローラーがまだなければ作成（非表示状態で待機）
            if (designerController == null)
            {
                var go = new GameObject("SDC_Designer_Controller");
                designerController = go.AddComponent<DesignerController>();
                // 最初は非表示にしておく場合はSetActive(false)だが、
                // DesignerControllerのStartでUIを作る構成なら、
                // ToggleDesignerでGameObjectを作る/破棄する方式の方が管理しやすい。
                // 今回は「トグルで生成/破棄」の挙動を維持しつつ、
                // HelperUIだけは常駐させる形（SongDataChangerの構成）に合わせる。

                // ただしSongDataChangerはSceneLoadedでMenuUIを自動生成している。
                // デザイナーなので、自動生成はせず、ボタンを押したときだけ生成するようにする。
                // ここでは「準備完了」状態にするだけ。
            }
        }

        private void ToggleDesigner()
        {
            // マッパーシーンでないなら何もしない
            if (SceneManager.GetActiveScene().buildIndex != 3) return;

            if (designerController != null)
            {
                UnityEngine.Object.Destroy(designerController.gameObject);
                designerController = null;
            }
            else
            {
                var go = new GameObject("UIPluginDesigner_Controller");
                designerController = go.AddComponent<DesignerController>();
            }
        }
    }
}
