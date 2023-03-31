using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PersonGroup : MonoBehaviour
{
    [SerializeField]
    GenericPersonAi[] members;    

    [SerializeField]
    MemberState[] memberStates;

    int activeCount;

    public int Count { get => members.Length; }

    public GenericPersonAi[] Members { get => members; set => members = value; }

    internal void GoToExit()
    {
        // find destination
        foreach (var member in members)
        {
            member.GoToExit();
        }
    }

    // [SerializeField] State state;

    public UnityEvent OnGroupTargetReached;

    enum State
    {
        Default,
        Moving,
        ReachedTarget
    }

    enum MemberState
    {
        Default,
        Moving,
        ReachedTarget,
        Deleted
    }

    public void SetMembers(GenericPersonAi[] members)
    {
        this.members = members;
        memberStates = new MemberState[members.Length];
    }

    public void MoveTo(Vector3 target) =>
        MoveTo(target, Vector3.zero);

    public void MoveTo(Vector3 target, Vector3 direction)
    {
        // state = State.Moving;

        for (int i = 0; i < members.Length; i++)
        {
            memberStates[i] = MemberState.Moving;
            members[i].MoveTo(target + direction * i * .3f, 0);
        }
    }

    public void OnTargetReched(GenericPersonAi person)
    {
        bool allReached = true;

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == person)
            {
                memberStates[i] = MemberState.ReachedTarget;
            }

            if (memberStates[i] != MemberState.ReachedTarget)
            {
                allReached = false;
            }
        }

        if (allReached)
        {
            // state = State.ReachedTarget;
            OnGroupTargetReached.Invoke();
        }

    }

    internal void OnMemberDelete(GenericPersonAi person)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == person)
            {
                members[i] = null;
                memberStates[i] = MemberState.Deleted;
                activeCount--;
                break;
            }
        }

        // if every member is deleted, destroy this group aswell
        if (activeCount <= 0)
        {
            Destroy(gameObject);
        }

    }


}
