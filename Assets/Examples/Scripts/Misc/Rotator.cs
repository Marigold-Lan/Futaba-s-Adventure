using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Info")] 
    public Space space;
    public Vector3 RotationEuler = new Vector3(0, 180, 0);

    protected virtual void LateUpdate()
    {
        transform.Rotate(RotationEuler * Time.deltaTime, space);
    }
    
}