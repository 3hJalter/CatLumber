using System;
using _Game.Managers;
using AudioEnum;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using TweenTypeEnum;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(HButton), typeof(CanvasGroup))]
public class ButtonAnim : HMonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private SfxType btnSound = SfxType.None;
    [SerializeField] private bool unscaleTime;
    private CanvasGroup _canvasGroup;
    private HButton _hcBtn;

    private void Awake()
    {
        _hcBtn = GetComponent<HButton>();
        _hcBtn.buttonAnim = this;
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Reset()
    {
        _hcBtn = GetComponent<HButton>();
        _hcBtn.transition = Selectable.Transition.None;
        _hcBtn.buttonAnim = this;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Ins.PlaySfx(btnSound);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_hcBtn.interactable)
            return;

        PointerDownAnim();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_hcBtn.interactable)
            return;

        PointerUpAnim();
    }

    public void ShowAnim(Action onComplete = null)
    {
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 1;
        Tf.DOKill();
        Tf.localScale = Vector3.one;

        if (showAnimType == ShowAnimType.None)
        {
            onComplete?.Invoke();
            return;
        }

        Ease doTweenEase = (Ease)showAnimEasingType;

        switch (showAnimType)
        {
            case ShowAnimType.FadeIn:
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = 0;

                    TweenerCore<float, float, FloatOptions> tween = _canvasGroup.DOFade(1, showAnimTime)
                        .SetUpdate(unscaleTime)
                        .OnComplete(() => onComplete?.Invoke());

                    if (UseShowAnimCurve)
                        tween.SetEase(showAnimCurve);
                    else
                        tween.SetEase(doTweenEase);
                }

                break;
            case ShowAnimType.FromScale:
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = 0;
                    _canvasGroup.DOFade(1, showAnimTime * .75f).SetUpdate(unscaleTime);
                }

                Tf.localScale = Vector3.one * initScale;

                if (separateAxisShowAnim)
                {
                    Ease doTweenXEase = (Ease)showAnimXAxisEasingType;
                    Ease doTweenYEase = (Ease)showAnimYAxisEasingType;

                    TweenerCore<Vector3, Vector3, VectorOptions> xTween = Tf.DOScaleX(1, showAnimTime)
                        .SetUpdate(unscaleTime)
                        .OnComplete(() => onComplete?.Invoke());

                    if (UseXAxisShowAnimCurve)
                        xTween.SetEase(showAnimXAxisCurve);
                    else
                        xTween.SetEase(doTweenXEase);

                    TweenerCore<Vector3, Vector3, VectorOptions> yTween = Tf.DOScaleY(1, showAnimTime)
                        .SetUpdate(unscaleTime);

                    if (UseYAxisShowAnimCurve)
                        yTween.SetEase(showAnimYAxisCurve);
                    else
                        yTween.SetEase(doTweenYEase);
                }
                else
                {
                    TweenerCore<Vector3, Vector3, VectorOptions> tween = Tf.DOScale(1, showAnimTime)
                        .SetUpdate(unscaleTime)
                        .OnComplete(() => onComplete?.Invoke());

                    if (UseShowAnimCurve)
                        tween.SetEase(showAnimCurve);
                    else
                        tween.SetEase(doTweenEase);
                }

                break;
        }
    }

    public void HideAnim(Action onComplete = null)
    {
        _canvasGroup.DOKill();
        Tf.DOKill();

        if (hideAnimType == HideAnimType.None)
        {
            onComplete?.Invoke();
            return;
        }

        Ease doTweenEase = (Ease)hideAnimEasingType;
        switch (hideAnimType)
        {
            case HideAnimType.FadeOut:
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = 1;

                    TweenerCore<float, float, FloatOptions> tween = _canvasGroup.DOFade(0, hideAnimTime)
                        .SetUpdate(unscaleTime)
                        .OnComplete(() => onComplete?.Invoke());

                    if (UseHideAnimCurve)
                        tween.SetEase(hideAnimCurve);
                    else
                        tween.SetEase(doTweenEase);
                }

                break;
            case HideAnimType.ToScale:
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = 1;
                    _canvasGroup.DOFade(0, hideAnimTime * .75f).SetUpdate(unscaleTime);
                }

                if (separateAxisHideAnim)
                {
                    Ease doTweenXEase = (Ease)hideAnimXAxisEasingType;
                    Ease doTweenYEase = (Ease)hideAnimYAxisEasingType;

                    TweenerCore<Vector3, Vector3, VectorOptions> xTween = Tf.DOScaleX(targetScale, hideAnimTime)
                        .SetUpdate(unscaleTime)
                        .OnComplete(() => onComplete?.Invoke());

                    if (UseXAxisHideAnimCurve)
                        xTween.SetEase(hideAnimXAxisCurve);
                    else
                        xTween.SetEase(doTweenXEase);

                    TweenerCore<Vector3, Vector3, VectorOptions> yTween = Tf.DOScaleY(targetScale, hideAnimTime)
                        .SetUpdate(unscaleTime);

                    if (UseYAxisHideAnimCurve)
                        yTween.SetEase(hideAnimYAxisCurve);
                    else
                        yTween.SetEase(doTweenYEase);
                }
                else
                {
                    TweenerCore<Vector3, Vector3, VectorOptions> tween = Tf.DOScale(targetScale, hideAnimTime)
                        .SetUpdate(unscaleTime)
                        .OnComplete(() => onComplete?.Invoke());

                    if (UseHideAnimCurve)
                        tween.SetEase(hideAnimCurve);
                    else
                        tween.SetEase(doTweenEase);
                }

                break;
        }
    }

    private void PointerDownAnim()
    {
        if (pressAnimType == PressAnimType.None)
            return;

        Tf.DOKill();
        Vector3 pressedTargetScaleI = pressedTargetScale;
        pressedTargetScaleI.z = 1;
        Ease doTweenEase = (Ease)pressAnimEasingType;

        switch (pressAnimType)
        {
            case PressAnimType.Scale:
                TweenerCore<Vector3, Vector3, VectorOptions> tween = Tf.DOScale(pressedTargetScaleI, pressAnimTime)
                    .SetUpdate(unscaleTime);

                if (UsePressAnimCurve)
                    tween.SetEase(pressAnimCurve);
                else
                    tween.SetEase(doTweenEase);

                break;
        }
    }

    private void PointerUpAnim()
    {
        if (pressAnimType == PressAnimType.None)
            return;

        Tf.DOKill();
        Ease doTweenEase = (Ease)releaseAnimEasingType;

        switch (pressAnimType)
        {
            case PressAnimType.Scale:
                TweenerCore<Vector3, Vector3, VectorOptions> tween = Tf.DOScale(Vector3.one, releaseAnimTime)
                    .SetUpdate(unscaleTime);

                if (UseReleaseAnimCurve)
                    tween.SetEase(releaseAnimCurve);
                else
                    tween.SetEase(doTweenEase);

                break;
        }
    }

    #region Enums

    private enum ShowAnimType
    {
        None,
        FadeIn,
        FromScale
    }

    private enum PressAnimType
    {
        None,
        Scale
    }

    private enum HideAnimType
    {
        None,
        FadeOut,
        ToScale
    }

    #endregion

    #region Show Properties

    #region Show Anim

    private bool ShowButtonAnim => Tf != null;

    private bool ShowShowAnimEasingType
    {
        get
        {
            if (showAnimType == ShowAnimType.FromScale && separateAxisShowAnim)
                return false;

            return showAnimType != ShowAnimType.None;
        }
    }

    private bool ShowSeparateShowAnimEasingType => showAnimType == ShowAnimType.FromScale && separateAxisShowAnim;

    private bool UseShowAnimCurve
    {
        get
        {
            if (ShowSeparateShowAnimEasingType)
                return false;

            return showAnimType != ShowAnimType.None && showAnimEasingType == EasingType.Custom;
        }
    }

    private bool UseXAxisShowAnimCurve =>
        ShowSeparateShowAnimEasingType && showAnimXAxisEasingType == EasingType.Custom;

    private bool UseYAxisShowAnimCurve =>
        ShowSeparateShowAnimEasingType && showAnimYAxisEasingType == EasingType.Custom;

    #endregion

    #region Press Anim

    private bool UsePressAnimCurve => pressAnimType != PressAnimType.None && pressAnimEasingType == EasingType.Custom;

    private bool UseReleaseAnimCurve =>
        pressAnimType != PressAnimType.None && releaseAnimEasingType == EasingType.Custom;

    #endregion

    #region Hide Anim

    private bool ShowHideAnimEasingType
    {
        get
        {
            if (hideAnimType == HideAnimType.ToScale && separateAxisHideAnim)
                return false;

            return hideAnimType != HideAnimType.None;
        }
    }

    private bool ShowSeparateHideAnimEasingType => hideAnimType == HideAnimType.ToScale && separateAxisHideAnim;

    private bool UseHideAnimCurve
    {
        get
        {
            if (ShowSeparateHideAnimEasingType)
                return false;

            return hideAnimType != HideAnimType.None && hideAnimEasingType == EasingType.Custom;
        }
    }

    private bool UseXAxisHideAnimCurve =>
        ShowSeparateHideAnimEasingType && hideAnimXAxisEasingType == EasingType.Custom;

    private bool UseYAxisHideAnimCurve =>
        ShowSeparateHideAnimEasingType && hideAnimYAxisEasingType == EasingType.Custom;

    #endregion

    #endregion

    #region Button Anim Fields

    #region Show Anim

    [Space] [Header("Show Anim")] [SerializeField] [ShowIf(nameof(ShowButtonAnim))]
    private ShowAnimType showAnimType = ShowAnimType.None;

    [SerializeField] [HideIf(nameof(showAnimType), ShowAnimType.None)]
    private float showAnimTime = .35f;

    [SerializeField] [ShowIf(nameof(showAnimType), ShowAnimType.FromScale)]
    private float initScale;

    [SerializeField] [ShowIf(nameof(showAnimType), ShowAnimType.FromScale)]
    private bool separateAxisShowAnim;

    [ShowIf(nameof(ShowShowAnimEasingType))]
    public EasingType showAnimEasingType = EasingType.OutQuad;

    [ShowIf(nameof(UseShowAnimCurve))] public AnimationCurve showAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [ShowIf(nameof(ShowSeparateShowAnimEasingType))]
    public EasingType showAnimXAxisEasingType = EasingType.OutQuad;

    [ShowIf(nameof(UseXAxisShowAnimCurve))]
    public AnimationCurve showAnimXAxisCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [ShowIf(nameof(ShowSeparateShowAnimEasingType))]
    public EasingType showAnimYAxisEasingType = EasingType.OutQuad;

    [ShowIf(nameof(UseYAxisShowAnimCurve))]
    public AnimationCurve showAnimYAxisCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #region Press Anim

    [Space] [Header("Press Anim")] [SerializeField] [ShowIf(nameof(ShowButtonAnim))]
    private PressAnimType pressAnimType = PressAnimType.None;

    [SerializeField] [HideIf(nameof(pressAnimType), PressAnimType.None)]
    private float pressAnimTime = .2f;

    [SerializeField] [HideIf(nameof(pressAnimType), PressAnimType.None)]
    private float releaseAnimTime = .2f;

    [SerializeField] [ShowIf(nameof(pressAnimType), PressAnimType.Scale)]
    private Vector2 pressedTargetScale = Vector2.one * .95f;

    [SerializeField] [HideIf(nameof(pressAnimType), PressAnimType.None)]
    private EasingType pressAnimEasingType = EasingType.OutQuad;

    [SerializeField] [ShowIf(nameof(UsePressAnimCurve))]
    private AnimationCurve pressAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] [HideIf(nameof(pressAnimType), PressAnimType.None)]
    private EasingType releaseAnimEasingType = EasingType.OutQuad;

    [SerializeField] [ShowIf(nameof(UseReleaseAnimCurve))]
    private AnimationCurve releaseAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #region Hide Anim

    [Space] [Header("Hide Anim")] [SerializeField] [ShowIf(nameof(ShowButtonAnim))]
    private HideAnimType hideAnimType = HideAnimType.None;

    [SerializeField] [HideIf(nameof(hideAnimType), HideAnimType.None)]
    private float hideAnimTime = .35f;

    [ShowIf(nameof(hideAnimType), HideAnimType.ToScale)]
    public float targetScale;

    [ShowIf(nameof(hideAnimType), HideAnimType.ToScale)]
    public bool separateAxisHideAnim;

    [ShowIf(nameof(ShowHideAnimEasingType))]
    public EasingType hideAnimEasingType = EasingType.OutQuad;

    [ShowIf(nameof(UseHideAnimCurve))] public AnimationCurve hideAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [ShowIf(nameof(ShowSeparateHideAnimEasingType))]
    public EasingType hideAnimXAxisEasingType = EasingType.OutQuad;

    [ShowIf(nameof(UseXAxisHideAnimCurve))]
    public AnimationCurve hideAnimXAxisCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [ShowIf(nameof(ShowSeparateHideAnimEasingType))]
    public EasingType hideAnimYAxisEasingType = EasingType.OutQuad;

    [ShowIf(nameof(UseYAxisHideAnimCurve))]
    public AnimationCurve hideAnimYAxisCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #endregion
}
