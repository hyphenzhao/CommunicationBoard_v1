// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity;
using UnityEngine;

namespace Academy
{
    public class TrackInteractibleAction : InteractibleAction
    {
        [SerializeField]
        [Tooltip("Drag the Tagalong prefab asset you want to display.")]
        private GameObject objectToTrack;

        public override void PerformAction()
        {
            // Recommend having only one tagalong.
            if (objectToTrack == null)
            {
                return;
            }
            objectToTrack.GetComponent<Tagalong>().enabled = true;
            objectToTrack.GetComponent<Billboard>().enabled = true;
        }
    }
}