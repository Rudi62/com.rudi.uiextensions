using UnityEngine;

namespace Rudi.UI
{
    public interface ISoftShadow
    {
        //public void rebuildShadowGraphic () ;
        //public ShadowProperties.Variables variables { get; set; }
        // public bool omitIfZeroDistance
        public bool showShadow { get; set; }
        public bool isEnabled { get; }
        public bool omitIfZeroDistance { get; set; }
        public float elevationMultiplier { get; set; }
        public void CrossFadeElevationMultiplier ( float dest , float duration );
        public void shadowChanged ();
        public void propertiesObjectChanged ( DropShadow.MessageReason reason );
        public MonoBehaviour getMonoObject ();
    }
}
