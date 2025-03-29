using DanieloZ.Transitions;
using DG.Tweening;
using Newtonsoft.Json.Serialization;
using Sirenix.OdinInspector;
using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace DanieloZ.Transitions
{
    //[CreateAssetMenu(
    //        fileName = "New_UI_Page_Transition",
    //        menuName = "Transitions/UI Page Transition",
    //        order = 1)]
    [Serializable]
    public class Transition_UI_Page : TransitionBase
    {
        [Space]
        public UI_Elements_Page page;
        public bool disablePageOnHide;

        [Header("Show transition")]
        public float onShowDuration;
        public Ease onShowEase = Ease.InOutQuad;

        [Header("Hide transition")]
        public float onHideDuration;
        public Ease onHideEase = Ease.InOutQuad;

        public override void CallTransition(TransitionsController controller, bool instantly = false)
        {
            CanvasGroup canvas = page.GetComponent<CanvasGroup>();
            UI_Management_Positioning positioning = page.GetComponent<UI_Management_Positioning>();
            if (page.Hidden)
            {
                if (instantly)
                {
                    canvas.alpha = 0;
                    positioning.SetPositionToOrigin();
                }
                else
                {
                    canvas.DOKill();
                    canvas.DOFade(0, onHideDuration).SetEase(onHideEase).OnComplete(() => { positioning.SetPositionToOrigin(); }).SetTarget(canvas);
                }

                canvas.interactable = false;
                canvas.blocksRaycasts = false;
            }
            else
            {
                positioning.SetPositionToTarget();
                if (instantly)
                {
                    canvas.alpha = 1;
                }
                else
                {
                    canvas.DOKill();
                    canvas.DOFade(1, onShowDuration).SetEase(onShowEase).SetTarget(canvas);
                }

                canvas.interactable = true;
                canvas.blocksRaycasts = true;
            }

            base.CallTransition(controller);
        }
    }
}
