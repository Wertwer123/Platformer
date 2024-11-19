using UnityEngine;

public class Stats : MonoBehaviour
{
   [SerializeField, Min(0.0f)] private float moveSpeed;
   [SerializeField, Min(0.0f)] private float acceleration;
   [SerializeField, Min(0.0f)] private float maxSpeed;

   public float GetMoveSpeed() => moveSpeed;
   public float GetAcceleration() => acceleration;
   public float GetMaxSpeed() => maxSpeed;
}
