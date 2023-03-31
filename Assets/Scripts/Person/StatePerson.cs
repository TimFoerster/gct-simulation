using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class StatePerson<T> : MonoBehaviour, IPerson where T : StatePerson<T>
{
    public abstract PersonMovement personMovement { get; set; }
    public abstract GenericPersonAi person { get; set; }
    public float Speed { set => personMovement.speed = value; }
    public bool OnEnterExit() => true;

    [SerializeField]
    protected Context context;
    protected void InitWithState(T obj, SimulationTime time, AbstractState state) =>
        context = new Context(obj, time, state);

    void FixedUpdate()
    {
        context.FixedUpdate();
    }


    [Serializable]
    protected class Context
    {
        enum State
        {
            Init,
            Idle,
            Moving,
            Waiting
        }

        // A reference to the current state of the Context.
        private AbstractState _state = null;
        public T p;
        public GenericPersonAi person => p.person;
        public SimulationTime time;
        [SerializeField]
        State localState;
        public float waitUntil;

        public Context(T obj, SimulationTime time, AbstractState state)
        {
            this.p = obj;
            this.time = time;
            this.TransitionTo(state);
        }

        // The Context allows changing the State object at runtime.
        public void TransitionTo(AbstractState state)
        {
            this._state = state;
            this._state.SetContext(this);
            localState = default;
        }

        public void OnInit()
        {
            this._state.OnInit();
        }

        public void OnArrive()
        {
            this._state.OnArrive();
        }

        public void OnTimePassed()
        {
            this._state.OnTimePassed();
        }

        public bool MoveTo(Vector3 position, int floor = 0)
        {
            localState = State.Moving;
            return person.MoveTo(position, floor);
        }

        public void Wait(float until)
        {
            localState = State.Waiting;
            waitUntil = until;
        }

        public void FixedUpdate()
        {
            switch (localState)
            {
                case State.Init: OnInit(); break;
                case State.Moving: if (p.personMovement.destinationReached) OnArrive(); break;
                case State.Waiting: if (waitUntil <= time.time) OnTimePassed(); break;
            }
        }

    }

    protected abstract class AbstractState
    {
        protected Context context;

        public void SetContext(Context context)
        {
            this.context = context;
        }

        public bool MoveTo(Vector3 position, int floor = 0) => 
            context.MoveTo(position, floor);

        public void WaitFor(float time) =>
            WaitUntil(context.time.time + time);

        public void WaitUntil(float time) =>
            context.Wait(time);

        public abstract void OnInit();

        public virtual void OnArrive() { }

        public virtual void OnTimePassed() { }
    }

    [Serializable]
    protected class MoveAndWaitState: AbstractState {

        Vector3 destination;
        float waitUntil;
        AbstractState nextState;

        public MoveAndWaitState(Vector3 destination, float waitUntil, AbstractState nextState)
        {
            this.destination = destination;
            this.waitUntil = waitUntil;
            this.nextState = nextState;
        }

        public override void OnArrive() =>
            WaitUntil(waitUntil);

        public override void OnInit() =>
            MoveTo(destination);

        public override void OnTimePassed() => 
            context.TransitionTo(nextState);
    }

    protected class LeaveState: AbstractState
    {
        public override void OnInit() =>
            context.person.GoToExit();
    }

}
