using System.Collections.Generic;
using _Game._Scripts.Managers;
using _Game.Camera;
using _Game.GameGrid;
using _Game.Managers;
using _Game.UIs.Screen;
using MEC;
using UnityEngine;
using UnityEngine.Playables;

public class FirstCutsceneHandler : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform boat;
    [SerializeField] private PlayableDirector playableDirector;
    
    
    public void OnStartCutscene()
    {
        Timing.RunCoroutine(OnPlayCutscene());
    }
    
    private IEnumerator<float> OnPlayCutscene()
    {
        CameraManager.Ins.ChangeCamera(ECameraType.CutsceneCamera);
        CameraManager.Ins.ChangeCameraTarget(ECameraType.CutsceneCamera, player);
        playableDirector.Play();
        yield return Timing.WaitForSeconds((float) playableDirector.duration);
        CameraManager.Ins.ChangeCamera(ECameraType.InGameCamera);
        LevelManager.Ins.HidePlayer(false);
        player.gameObject.SetActive(false);
        TutorialManager.Ins.AddCutsceneObject(boat);
        yield return Timing.WaitForSeconds(2f); // Time change camera
        TutorialManager.Ins.TutorialList[0].OnForceShowTutorial(0);
        Destroy(gameObject);
    }
}
