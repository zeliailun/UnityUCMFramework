using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [DisallowMultipleComponent]
    public class SceneCamera : MonoBehaviour
    {
        public bool onPlayingOff = true;

        private void Awake()
        {
            if (onPlayingOff)
                gameObject.SetActive(false);
        }
    }
}
