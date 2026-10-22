using System;
using UnityEngine;
using UnityEngine.UI;
namespace ACS.Animations
{
    public static class AnimationExtensions
    {
        private static AnimationHandle Vector(UnityEngine.Object target, Func<Vector3> read, Action<Vector3> write, Vector3 end, float duration)
        {
            Vector3 from=default, to=default; AnimationHandle handle=null;
            handle = new AnimationHandle(target,duration,()=>{ from=read();to=handle.Relative?from+end:end; },t=>write(Vector3.LerpUnclamped(from,to,t)));
            return handle;
        }
        private static AnimationHandle Float(UnityEngine.Object target, Func<float> read, Action<float> write, float end, float duration)
        {
            float from=0,to=0; AnimationHandle handle=null;
            handle=new AnimationHandle(target,duration,()=>{from=read();to=handle.Relative?from+end:end;},t=>write(Mathf.LerpUnclamped(from,to,t)));
            return handle;
        }
        public static AnimationHandle AnimateMove(this Transform target, Vector3 end, float duration) => Vector(target,()=>target.position,v=>target.position=v,end,duration);
        public static AnimationHandle AnimateLocalMove(this Transform target, Vector3 end, float duration) => Vector(target,()=>target.localPosition,v=>target.localPosition=v,end,duration);
        public static AnimationHandle AnimateLocalMoveY(this Transform target, float end, float duration) => Float(target,()=>target.localPosition.y,v=>{var p=target.localPosition;p.y=v;target.localPosition=p;},end,duration);
        public static AnimationHandle AnimateAnchorPos(this RectTransform target, Vector2 end, float duration) => Vector(target,()=>target.anchoredPosition,v=>target.anchoredPosition=v,end,duration);
        public static AnimationHandle AnimateAnchorPosY(this RectTransform target, float end, float duration) => Float(target,()=>target.anchoredPosition.y,v=>{var p=target.anchoredPosition;p.y=v;target.anchoredPosition=p;},end,duration);
        public static AnimationHandle AnimateAnchoredPosition(this RectTransform target, Vector2 end, float duration) => target.AnimateAnchorPos(end,duration);
        public static AnimationHandle AnimateLocalRotation(this Transform target, Vector3 end, float duration) => target.AnimateLocalRotate(end,duration);
        public static AnimationHandle AnimateScale(this Transform target, Vector3 end, float duration) => Vector(target,()=>target.localScale,v=>target.localScale=v,end,duration);
        public static AnimationHandle AnimateScale(this Transform target, float end, float duration) => target.AnimateScale(Vector3.one*end,duration);
        public static AnimationHandle AnimateRotate(this Transform target, Vector3 end, float duration) => Rotate(target,end,duration,false);
        public static AnimationHandle AnimateLocalRotate(this Transform target, Vector3 end, float duration) => Rotate(target,end,duration,true);
        private static AnimationHandle Rotate(Transform target, Vector3 end,float duration,bool local)
        {
            Vector3 from=default,delta=default;AnimationHandle handle=null;
            handle=new AnimationHandle(target,duration,()=>
            {
                from=local?target.localEulerAngles:target.eulerAngles;
                delta=handle.Relative?end:new Vector3(Mathf.DeltaAngle(from.x,end.x),Mathf.DeltaAngle(from.y,end.y),Mathf.DeltaAngle(from.z,end.z));
            },t=>{var q=Quaternion.Euler(from+delta*t);if(local)target.localRotation=q;else target.rotation=q;});
            return handle;
        }
        public static AnimationHandle AnimateFade(this Graphic target,float alpha,float duration) => Float(target,()=>target.color.a,v=>{var c=target.color;c.a=v;target.color=c;},alpha,duration);
        public static AnimationHandle AnimateFade(this CanvasGroup target,float alpha,float duration) => Float(target,()=>target.alpha,v=>target.alpha=v,alpha,duration);
        public static AnimationHandle AnimateFillAmount(this Image target,float amount,float duration) => Float(target,()=>target.fillAmount,v=>target.fillAmount=v,amount,duration);
        public static AnimationHandle AnimateValue(this Slider target,float value,float duration) => Float(target,()=>target.value,v=>target.value=v,value,duration);
        public static AnimationHandle AnimateColor(this Graphic target,Color color,float duration)
        {
            Color from=default,to=default;AnimationHandle handle=null;
            handle=new AnimationHandle(target,duration,()=>{from=target.color;to=handle.Relative?from+color:color;},t=>target.color=Color.LerpUnclamped(from,to,t));
            return handle;
        }
        public static AnimationHandle AnimatePunch(this Transform target,Vector3 strength,float duration,int vibrations=5)
        {
            Vector3 start=default;return new AnimationHandle(target,duration,()=>start=target.localScale,t=>target.localScale=start+strength*(Mathf.Sin(t*Mathf.PI*2*Mathf.Max(1,vibrations))*(1-t))).SetEase(Ease.Linear);
        }
        public static AnimationHandle AnimateShake(this Transform target,Vector3 strength,float duration,int vibrations=10)
        {
            Vector3 start=default;return new AnimationHandle(target,duration,()=>start=target.localPosition,t=>
            {
                float a=t*Mathf.PI*2*Mathf.Max(1,vibrations);
                target.localPosition=start+Vector3.Scale(strength,new Vector3(Mathf.Sin(a),Mathf.Sin(a*1.37f),Mathf.Sin(a*1.79f)))*(1-t);
            }).SetEase(Ease.Linear);
        }
        public static void CancelAnimations(this UnityEngine.Object target) => AnimationRunner.Cancel(target);
    }
}
