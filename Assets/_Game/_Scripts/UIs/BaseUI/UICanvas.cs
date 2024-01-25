using System;
using _Game.Managers;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class UICanvas : MonoBehaviour
{
    //public bool IsAvoidBackKey = false;
    [FormerlySerializedAs("IsDestroyOnClose")]
    public bool isDestroyOnClose;

    [SerializeField] private bool useAnimator;

    [ShowIf("useAnimator")] [SerializeField]
    private Animator animator;

    private string _currentAnim = " ";

    private RectTransform _mRectTransform;

    protected RectTransform MRectTransform
    {
        get
        {
            _mRectTransform = _mRectTransform ? _mRectTransform : gameObject.transform as RectTransform;
            return _mRectTransform;
        }
    }

    public virtual void Setup()
    {
        UIManager.Ins.AddBackUI(this);
        UIManager.Ins.PushBackAction(this, BackKey);
    }

    protected virtual void BackKey()
    {

    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        //anim
        if (useAnimator) OpenAnimationAnim();
    }

    private void OpenAnimationAnim()
    {
        ChangeAnim(Constants.OPEN);
    }

    private void CloseAnimationAnim(Action onCompleteAction, float time = 0.2f)
    {
        ChangeAnim(Constants.CLOSE);
        DOVirtual.DelayedCall(time, () => onCompleteAction?.Invoke());
    }

    public virtual void Close()
    {
        UIManager.Ins.RemoveBackUI(this);
        //anim
        if (useAnimator) CloseAnimationAnim(OnClose);
        else OnClose();
    }

    public virtual void CloseDirectly()
    {
        if (UIManager.Ins.IsContain(this))
        {
            UIManager.Ins.RemoveBackUI(this);
        }
        if (useAnimator) CloseAnimationAnim(OnClose);
        else OnClose();
    }
    
    private void OnClose()
    {
        gameObject.SetActive(false);
        if (isDestroyOnClose) Destroy(gameObject);
    }

    private void ChangeAnim(string animName)
    {
        animator.ResetTrigger(_currentAnim);
        _currentAnim = animName;
        animator.SetTrigger(_currentAnim);
    }
}
