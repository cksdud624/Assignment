using UnityEngine;

namespace Common
{
    public class Bootstrap : MonoBehaviour
    {
        private void Start()
        {
            Global.Instance.Init();
        }
    }
}