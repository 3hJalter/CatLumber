using _Game.DesignPattern;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class InitManager : Singleton<InitManager>
{
    #region PROPERTYS
    [SerializeField]
    bool isDebug = false;
    [SerializeField]
    bool isDebugGridLogic = false;
    [SerializeField]
    bool isDebugFps = false;
    [SerializeField]
    bool isDebugLog = false;
    [SerializeField]
    GameObject debugObject;
    [SerializeField]
    InitCanvas debugCanvas;
    DebugManager debugManager;
    #endregion
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        debugCanvas._OnToggleValueChange += OnSetDebug;
        debugCanvas._OnStartGame += OnStartGame;
        debugCanvas.SetData(isDebugGridLogic, isDebugFps, isDebugLog);
        
    }

    private void OnSetDebug(int id, bool value)
    {
        switch (id)
        {
            case 0:
                isDebugGridLogic = value;
                break;
            case 1:
                isDebugFps = value;
                break;
            case 2:
                isDebugLog = value;
                break;
        }
    }

    private void OnStartGame()
    {
        if (isDebug)
        {
            debugManager = Instantiate(debugObject).GetComponent<DebugManager>();
            debugManager.OnInit(isDebugGridLogic, isDebugFps, isDebugLog, debugCanvas.StartLevel);
            DontDestroyOnLoad(debugManager.gameObject);
        }
        SceneManager.LoadScene(1);
    }
}
