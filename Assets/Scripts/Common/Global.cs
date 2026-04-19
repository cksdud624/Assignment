using System.Collections.Generic;
using Common.Scene;
using Common.Scene.Parameter;
using Common.Template;
using Common.Template.Interface;
using Cysharp.Threading.Tasks;
using Generated.Table;
using UnityEngine;

namespace Common
{
    public class Global : Singleton<Global>
    {
        private readonly List<IUpdateable> _updateables = new ();
        private readonly List<IFixedUpdateable> _fixedUpdateables = new ();
        
        public SceneLoader SceneLoader { get; private set; }
        public TableManager TableManager { get; private set; }
        public AssetManager AssetManager { get; private set; }
        
        public void Init() => Load().Forget();

        private async UniTask Load()
        {
            TableManager = InitGlobal<TableManager>();
            await TableManager.Init();
            AssetManager = InitGlobal<AssetManager>();
            
            SceneLoader = InitGlobal<SceneLoader>();
            
            var mainParameter = ScriptableObject.CreateInstance<SceneParameterMain>();
            mainParameter.Init();
            SceneLoader.LoadScene(GameDefine.SceneType.Main, mainParameter);
        }

        private T InitGlobal<T>() where T : MonoBehaviour
        {
            GameObject dontDestroyObject = new GameObject(typeof(T).Name);
            T component = dontDestroyObject.AddComponent<T>();
            dontDestroyObject.transform.SetParent(Instance.transform);
            return component;
        }

        #region LifeCycle
        private void Update()
        {
            for (int i = _updateables.Count - 1; i >= 0; i--)
            {
                _updateables[i].OnUpdate();
            }
        }

        private void FixedUpdate()
        {
            for(int i = _fixedUpdateables.Count - 1; i >= 0; i--)
            {
                _fixedUpdateables[i].OnFixedUpdate();
            }
        }
        #endregion

        #region Bind Events
        public void BindUpdate(IUpdateable updateable) => _updateables.Add(updateable);
        public void UnBindUpdate(IUpdateable updateable) => _updateables.Remove(updateable);
        public void BindFixedUpdate(IFixedUpdateable updateable) => _fixedUpdateables.Add(updateable);
        public void UnBindFixedUpdate(IFixedUpdateable updateable) => _fixedUpdateables.Remove(updateable);
        #endregion
    }
}
