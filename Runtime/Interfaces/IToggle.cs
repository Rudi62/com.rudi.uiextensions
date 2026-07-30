using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.UI
{
    public interface IToggle : ISelectable
    {
        public bool isOn { get; set; }
        public void SetIsOnWithoutNotify ( bool value );

    }
}
