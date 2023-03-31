using System.Collections;
using UnityEngine;

public abstract class INeed : MonoBehaviour
{
    public abstract bool HasNeed();
    public abstract bool CanFullfillNeed();
    public abstract void FullfillNeed();
    public abstract bool IsNeedCompleted();
    public abstract void AfterNeedCompleted();
}
