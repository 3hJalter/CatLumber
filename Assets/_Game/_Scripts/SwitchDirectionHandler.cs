using UnityEngine;

public class SwitchDirectionHandler : MonoBehaviour
{
    [Tooltip("0: Left, 1: Right, 2: Up, 3: Down")] [SerializeField]
    private GameObject[] objectsToHide;

    private GameObject _currentActiveObject;

    private void Awake()
    {
        for (int i = 0; i < objectsToHide.Length; i++) objectsToHide[i].SetActive(false);
        _currentActiveObject = objectsToHide[0];
    }

    public void Reset()
    {
        _currentActiveObject.SetActive(false);
    }

    public void ShowObject(Direction direction)
    {
        if (direction is Direction.None)
        {
            if (_currentActiveObject.activeSelf) Reset();
            return;
        }

        if (_currentActiveObject == objectsToHide[(int)direction]) return;
        _currentActiveObject.SetActive(false);
        _currentActiveObject = objectsToHide[(int)direction];
        _currentActiveObject.SetActive(true);
    }
}
