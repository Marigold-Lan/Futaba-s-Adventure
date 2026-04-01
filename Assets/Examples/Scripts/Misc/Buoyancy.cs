using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    public float force = 10f;
    
    protected Rigidbody rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(GameTags.VolumeWater))
        {
            if (transform.position.y < other.bounds.max.y)
            {
                var multiplier = Mathf.Clamp01(other.bounds.max.y - transform.position.y);
                var buoyancy = Vector3.up * force * multiplier;
                rb.AddForce(buoyancy);
            }
        }
    }
}