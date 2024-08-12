using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JetMod
{
    public class routine : MonoBehaviour
    {
        public IEnumerator CoroutineDelay()
        {
            yield return null;
        }

        public void startDelay()
        {
            StartCoroutine(CoroutineDelay());
        }
    }
}
