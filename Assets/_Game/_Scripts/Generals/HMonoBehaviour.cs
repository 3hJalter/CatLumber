using Sirenix.OdinInspector;
using UnityEngine;

public class HMonoBehaviour : SerializedMonoBehaviour
{
    private Transform _tf;

    public Transform Tf
    {
        get
        {
            _tf = _tf ? _tf : gameObject.transform;
            return _tf;
        }
    }
}
