using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Academy {
    public class CheckIntersaction : MonoBehaviour {
        int counter;
        int threshold = 20;
        [SerializeField]
        private GameObject gazePoint;

        private Transform preWord;

        // Use this for initialization
        void Start() {
            counter = 0;
        }

        // Update is called once per frame
        void Update() {
            if (gazePoint != null && gazePoint.activeSelf)
            {
                Collider gazeCollider = gazePoint.GetComponent<Collider>();
                Collider wordCollider = null;
                if (counter < threshold)
                {
                    foreach(Transform word in transform)
                    {
                       if(word != null && word.name.StartsWith("Words-"))
                        {
                            wordCollider = word.GetComponent<Collider>();
                            if (gazeCollider.bounds.Intersects(wordCollider.bounds))
                            {
                                if(preWord != null && string.Equals(preWord.name, word.name))
                                {
                                    counter++;
                                }
                                else
                                {
                                    counter = 1;
                                    preWord = word;
                                }
                                // Debug.Log(word.name + ":" + counter);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    counter = 0;
                    preWord.GetComponent<Interactible>().PlaySound();
                }
            }
        }
    }
}
