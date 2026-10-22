using UnityEngine;
namespace ACS.Animations
{
    public static class AnimationEasing
    {
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0 || t >= 1) return t;
            switch (ease)
            {
                case Ease.Linear: return t;
                case Ease.InSine: return 1 - Mathf.Cos(t * Mathf.PI / 2);
                case Ease.OutSine: return Mathf.Sin(t * Mathf.PI / 2);
                case Ease.InOutSine: return (1 - Mathf.Cos(Mathf.PI * t)) / 2;
                case Ease.InQuad: return t*t;
                case Ease.Unset: case Ease.OutQuad: return 1-(1-t)*(1-t);
                case Ease.InOutQuad: return t < .5f ? 2*t*t : 1-Mathf.Pow(-2*t+2,2)/2;
                case Ease.InCubic: return t*t*t;
                case Ease.OutCubic: return 1-Mathf.Pow(1-t,3);
                case Ease.InOutCubic: return t < .5f ? 4*t*t*t : 1-Mathf.Pow(-2*t+2,3)/2;
                case Ease.InQuart: return Mathf.Pow(t,4);
                case Ease.OutQuart: return 1-Mathf.Pow(1-t,4);
                case Ease.InOutQuart: return t < .5f ? 8*Mathf.Pow(t,4) : 1-Mathf.Pow(-2*t+2,4)/2;
                case Ease.InQuint: return Mathf.Pow(t,5);
                case Ease.OutQuint: return 1-Mathf.Pow(1-t,5);
                case Ease.InOutQuint: return t < .5f ? 16*Mathf.Pow(t,5) : 1-Mathf.Pow(-2*t+2,5)/2;
                case Ease.InExpo: return Mathf.Pow(2,10*t-10);
                case Ease.OutExpo: return 1-Mathf.Pow(2,-10*t);
                case Ease.InOutExpo: return t < .5f ? Mathf.Pow(2,20*t-10)/2 : (2-Mathf.Pow(2,-20*t+10))/2;
                case Ease.InCirc: return 1-Mathf.Sqrt(1-t*t);
                case Ease.OutCirc: return Mathf.Sqrt(1-(t-1)*(t-1));
                case Ease.InOutCirc: return t < .5f ? (1-Mathf.Sqrt(1-4*t*t))/2 : (Mathf.Sqrt(1-Mathf.Pow(-2*t+2,2))+1)/2;
                case Ease.InBack: return 2.70158f*t*t*t-1.70158f*t*t;
                case Ease.OutBack: return 1+2.70158f*Mathf.Pow(t-1,3)+1.70158f*Mathf.Pow(t-1,2);
                case Ease.InOutBack:
                    const float c = 2.5949095f;
                    return t < .5f ? Mathf.Pow(2*t,2)*((c+1)*2*t-c)/2 : (Mathf.Pow(2*t-2,2)*((c+1)*(2*t-2)+c)+2)/2;
                case Ease.InBounce: return 1-Bounce(1-t);
                case Ease.OutBounce: return Bounce(t);
                case Ease.InOutBounce: return t < .5f ? (1-Bounce(1-2*t))/2 : (1+Bounce(2*t-1))/2;
                case Ease.InElastic: return Elastic(t, true, false);
                case Ease.OutElastic: return Elastic(t, false, false);
                case Ease.InOutElastic: return Elastic(t, false, true);
                // Existing ACS uses the default single-flash setting (one forward transition).
                case Ease.Flash: return t;
                case Ease.InFlash: return t*t;
                case Ease.OutFlash: return t*(2-t);
                case Ease.InOutFlash: return t<.5f?2*t*t:1-Mathf.Pow(-2*t+2,2)/2;
                default: return t;
            }
        }
        private static float Elastic(float t, bool inward, bool both)
        {
            const float amplitude = 1.70158f;
            float period = both ? .45f : .3f;
            float phase = period / (2 * Mathf.PI) * Mathf.Asin(1 / amplitude);
            if (both)
            {
                float u = 2 * t - 1;
                return u < 0 ? -.5f * amplitude * Mathf.Pow(2, 10 * u) * Mathf.Sin((u - phase) * 2 * Mathf.PI / period)
                    : .5f * amplitude * Mathf.Pow(2, -10 * u) * Mathf.Sin((u - phase) * 2 * Mathf.PI / period) + 1;
            }
            if (inward) { t -= 1; return -amplitude * Mathf.Pow(2, 10 * t) * Mathf.Sin((t - phase) * 2 * Mathf.PI / period); }
            return amplitude * Mathf.Pow(2, -10 * t) * Mathf.Sin((t - phase) * 2 * Mathf.PI / period) + 1;
        }
        private static float Bounce(float t)
        {
            const float n=7.5625f,d=2.75f;
            if(t<1/d)return n*t*t;
            if(t<2/d){t-=1.5f/d;return n*t*t+.75f;}
            if(t<2.5f/d){t-=2.25f/d;return n*t*t+.9375f;}
            t-=2.625f/d;return n*t*t+.984375f;
        }
    }
}
