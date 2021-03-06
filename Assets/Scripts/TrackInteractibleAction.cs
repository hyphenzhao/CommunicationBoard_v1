﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        [SerializeField]
        private GameObject acitiveObject1;
        [SerializeField]
        private GameObject acitiveObject2;
        [SerializeField]
        private GameObject acitiveObject3;
        [SerializeField]
        private GameObject acitiveObject4;

        private bool trackEnabled = false;

        public override void PerformAction()
        {
            // Recommend having only one tagalong.
            if (objectToTrack == null)
            {
                return;
            }
            if (trackEnabled)
            {
                objectToTrack.GetComponent<TCPClientSide>().enabled = false;
                objectToTrack.GetComponent<CheckIntersaction>().enabled = false;
                acitiveObject1.SetActive(false);
                acitiveObject2.SetActive(false);
                acitiveObject3.SetActive(false);
                acitiveObject4.GetComponent<CalibrationRefineInteractibleAction>().enabled = false;
                trackEnabled = false;
            }
            else
            {
                objectToTrack.GetComponent<TCPClientSide>().enabled = true;
                objectToTrack.GetComponent<CheckIntersaction>().enabled = true;
                acitiveObject1.SetActive(true);
                acitiveObject2.SetActive(true);
                acitiveObject3.SetActive(true);
                acitiveObject4.GetComponent<CalibrationRefineInteractibleAction>().enabled = true;
                trackEnabled = true;
            }
        }
    }
}