using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Academy
{
    public class CalibrateInteractibleAction : InteractibleAction
    {
        [SerializeField]
        private GameObject activateObject;
        [SerializeField]
        private GameObject deactivateObject;

        public override void PerformAction()
        {
            activateObject.SetActive(true);
            deactivateObject.SetActive(false);
        }
    }
}
