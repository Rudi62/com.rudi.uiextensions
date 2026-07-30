using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    public interface ISelectable
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
    }
}
