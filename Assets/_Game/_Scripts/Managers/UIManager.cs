using System;
using System.Collections.Generic;
using System.Linq;
using _Game.DesignPattern;
using UnityEngine;
using UnityEngine.Events;

namespace _Game.Managers
{
    public class UIManager : Singleton<UIManager>
    {
        public Transform canvasParentTf;

        //dict for UI active
        private readonly Dictionary<Type, UICanvas> uiCanvas = new();

        //dict for quick query UI prefab
        private readonly Dictionary<Type, UICanvas> uiCanvasPrefab = new();

        //list from resource
        private UICanvas[] uiResources;

        #region Canvas

        public T OpenUI<T>() where T : UICanvas
        {
            UICanvas canvas = GetUI<T>();
            canvas.Setup();
            canvas.Open();

            return (T)canvas;
        }
        
        public UICanvas OpenUIDirectly(UICanvas ui)
        {
            UICanvas canvas = Instantiate(ui, canvasParentTf);
            canvas.Setup();
            canvas.Open();
            return canvas;
        }

        public void CloseUI<T>() where T : UICanvas
        {
            if (IsOpened<T>()) GetUI<T>().Close();
        }
        
        public void CloseUIDirectly(UICanvas ui)
        {
            if (!ui.gameObject.activeInHierarchy) return;
            ui.CloseDirectly();
        }

        public bool IsOpened<T>() where T : UICanvas
        {
            return IsLoaded<T>() && uiCanvas[typeof(T)].gameObject.activeInHierarchy;
        }
        
        public bool IsContain(UICanvas ui)
        {
            return uiCanvas.ContainsValue(ui);
        }


        public bool IsLoaded<T>() where T : UICanvas
        {
            Type type = typeof(T);
            return uiCanvas.ContainsKey(type) && uiCanvas[type] != null;
        }

        public T GetUI<T>() where T : UICanvas
        {
            if (!IsLoaded<T>())
            {
                UICanvas canvas = Instantiate(GetUIPrefab<T>(), canvasParentTf);
                uiCanvas[typeof(T)] = canvas;
            }

            return uiCanvas[typeof(T)] as T;
        }


        private T GetUIPrefab<T>() where T : UICanvas
        {
            if (uiCanvasPrefab.ContainsKey(typeof(T))) return uiCanvasPrefab[typeof(T)] as T;
            uiResources ??= Resources.LoadAll<UICanvas>(Constants.UI_PATH);

            for (int i = 0; i < uiResources.Length; i++)
                if (uiResources[i] is T)
                {
                    uiCanvasPrefab[typeof(T)] = uiResources[i];
                    break;
                }

            return uiCanvasPrefab[typeof(T)] as T;
        }

        #endregion

        #region Back Button

        private readonly Dictionary<UICanvas, UnityAction> backActionEvents = new();
        private readonly List<UICanvas> backCanvas = new();

        private UICanvas BackTopUI
        {
            get
            {
                UICanvas canvas = null;
                if (backCanvas.Count > 0) canvas = backCanvas[^1];

                return canvas;
            }
        }


        // private void LateUpdate()
        // {
        //     if (Input.GetKey(KeyCode.Escape) && BackTopUI != null)
        //     {
        //         BackActionEvents[BackTopUI]?.Invoke();
        //     }
        // }

        public void PushBackAction(UICanvas canvas, UnityAction action)
        {
            backActionEvents.TryAdd(canvas, action);
        }

        public void AddBackUI(UICanvas canvas)
        {
            if (!backCanvas.Contains(canvas)) backCanvas.Add(canvas);
        }

        public void RemoveBackUI(UICanvas canvas)
        {
            backCanvas.Remove(canvas);
        }

        public void CloseAll()
        {
            foreach (KeyValuePair<Type, UICanvas> item in uiCanvas.Where(item =>
                         item.Value != null && item.Value.gameObject.activeInHierarchy))
                item.Value.Close();
        }


        /// <summary>
        ///     CLear back key when comeback index UI canvas
        /// </summary>
        public void ClearBackKey()
        {
            backCanvas.Clear();
        }

        #endregion
    }
}
