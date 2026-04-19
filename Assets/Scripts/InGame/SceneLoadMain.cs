using Common;
using Common.Scene;
using Common.Scene.Parameter;
using Common.Template.Interface;
using InGame.Model;
using UnityEngine;

namespace InGame
{
    public class SceneLoadMain : SceneLoadBase, ISceneParameter<SceneParameterMain>
    {
        [SerializeField] private GameController gameController;
        private SceneParameterMain _sceneParameterMain;
        private InGameModel _inGameModel;
        protected void Awake() => Global.Instance.SceneLoader.SetCurrentScene<SceneParameterMain>(this);
        
        public override void InitScene()
        {
            if (_sceneParameterMain == null)
            {
                Debug.LogError($"{typeof(SceneParameterMain)} is null");
                return;
            }
            
            base.InitScene();
            _inGameModel = new InGameModel(_sceneParameterMain);
            gameController.Init(_inGameModel);
        }

        public void SetParameter(SceneParameterMain parameter) => _sceneParameterMain = parameter;

        public override void DisposeScene()
        {
            base.DisposeScene();
            _inGameModel?.Release();
        }
    }
}
