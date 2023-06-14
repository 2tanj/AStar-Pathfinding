using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Unit : MonoBehaviour
{
    private SpriteRenderer _sprite;

    private void Start()
    {
        _sprite = GetComponent<SpriteRenderer>();
    }

    public void ChangePosition(Transform newPos)
    {
        transform.SetParent(newPos);
        transform.localPosition = Vector3.zero;
    }
}
