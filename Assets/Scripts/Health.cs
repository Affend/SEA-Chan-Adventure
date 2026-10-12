using System;
using UnityEngine;

// Komponen nyawa (HP) yang bisa dipakai player maupun musuh.
public class Health : MonoBehaviour
{
    public float Max { get; private set; }
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public event Action<Health> Changed;
    public event Action<Health> Died;

    public void Init(float max)
    {
        Max = Mathf.Max(1f, max);
        Current = Max;
        Changed?.Invoke(this);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke(this);
        if (Current <= 0f) Died?.Invoke(this);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(Max, Current + amount);
        Changed?.Invoke(this);
    }

    public void AddMax(float amount, bool healToo)
    {
        Max += amount;
        Current = healToo ? Current + amount : Mathf.Min(Current, Max);
        Changed?.Invoke(this);
    }
}
