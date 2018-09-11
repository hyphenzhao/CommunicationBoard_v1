using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine;

namespace Academy
{
    public class TagAlongInteractibleAction : InteractibleAction
    {
        [SerializeField]
        [Tooltip("Drag the Tagalong prefab asset you want to display.")]
        private GameObject objectToTrack;
        [SerializeField]
        private GameObject fixedObject;

        private bool trackEnabled = false;
        private Vector3 orignalVector;
        private Quaternion orignalRotation;

        public override void PerformAction()
        {
            // Recommend having only one tagalong.
            if (objectToTrack == null)
            {
                return;
            }
            if (trackEnabled)
            {
                objectToTrack.GetComponent<Tagalong>().enabled = false;
                objectToTrack.GetComponent<Billboard>().enabled = false;
                //objectToTrack.transform.rotation = orignalRotation;
                //objectToTrack.transform.position = orignalVector;
                trackEnabled = false;
                objectToTrack.transform.position = fixedObject.transform.position;
                objectToTrack.transform.rotation = fixedObject.transform.rotation;
            }
            else
            {
                //float x = objectToTrack.transform.position.x;
                //float y = objectToTrack.transform.position.y;
                //float z = objectToTrack.transform.position.z;
                //orignalVector = new Vector3(x, y, z);
                //orignalRotation = objectToTrack.transform.rotation;
                objectToTrack.GetComponent<Tagalong>().enabled = true;
                objectToTrack.GetComponent<Billboard>().enabled = true;
                trackEnabled = true;
            }
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
