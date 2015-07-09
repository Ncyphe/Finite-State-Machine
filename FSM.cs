using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum FSMDefault { ROOT = -1 }


//Stackable Finite State Machine
//Pushes all states into a stack so that states can be shifted into previous states.

enum StateChangeType { NOT_ASSIGNED = -1 }

public class FSM
{
    Dictionary<System.Enum, StateBase> _StateCache;

    StateBase _CurrentState = null;

    public FSM()
    {
        _StateCache = new Dictionary<System.Enum, StateBase>();     //Registry Of All Available States, Sorted By Enum Int
        
        _CurrentState = new StateBase();
    }

    public void Update()
    {
        _CurrentState.OnUpdate();
    }

    public void FixedUpdate()
    {
        _CurrentState.OnFixedUpdate();
    }

    public void MessageState(System.Enum msgType, params System.Object[] msgData)
    {
        _CurrentState.OnMessage(msgType, msgData);
    }

    //Push A State Onto The Stack From Cache
    public bool PushState(System.Enum stateID)
    {
        StateBase newState;
        //check if the stateID exists
        if (!_StateCache.TryGetValue(stateID, out newState))
        {
            //stateID does not exist, back out
            Debug.Log("FSM Error: Can't Push State, [" + stateID.ToString() + "] does not exist in the cache");
            return false;
        }
        //Check if the prevState attribute is already set, identifying a state already in use
        if (newState.PrevState != null)
        {
            Debug.Log("FSM Error: Can't Push State, [" + stateID.ToString() + "] already in use");
            return false;
        }

        PushState(newState);

        return true;
    }

    //Push A State Onto The Stack By Reference
    public void PushState(StateBase newState)
    {
        _CurrentState.OnSuspend();

        newState.PrevState = _CurrentState;
        _CurrentState.NextState = newState;
        _CurrentState = newState;

        _CurrentState.OnEnter();
    }

    //Remove Top State From Stack
    public void PopState()
    {
        if (_CurrentState.PrevState == null)
        {
            Debug.Log("FSM Error: Trying to pop an empty cache");
            return;
        }

        _CurrentState.OnExit();
        _CurrentState = _CurrentState.PrevState;
        _CurrentState.PrevState = null;
        _CurrentState.NextState = null;
        _CurrentState.OnWakeUp();
    }

    //Replace Top Most State With A New State In Cache
    public bool ChangeState(System.Enum stateID)
    {
        StateBase nextState;
        if (!_StateCache.TryGetValue(stateID, out nextState))   //FAIL If stateID Does Not Exist
        {
            Debug.Log("FSM Error: Can't Change To State, [" + stateID.ToString() + "] does not exist in the cache");
            return false;
        }

        ChangeState(nextState);

        return true;
    }

    //Replace Top Most State With A New State By Reference
    public void ChangeState(StateBase nextState)
    {
        _CurrentState.OnExit();                             //Call state OnExit()
        nextState.PrevState = _CurrentState.PrevState;      //set nextState's PrevState to the Previous State in line
        _CurrentState.PrevState = null;                     //Empty the old states PrevState
        _CurrentState = nextState;                          //Set the _CurrentState to NextState
        _CurrentState.PrevState.NextState = _CurrentState;  //Set the Previous state's NextState to the new _CurrentState
        _CurrentState.OnEnter();                            //call the new _CurrentState's OnEnter()
    }

    //Add A New State To Cache
    public void RegisterState(System.Enum stateID, StateBase newState)
    {
        if (_StateCache.ContainsKey(stateID)) //FAIL if stateID Is Already In Use
        {
            Debug.Log("FSM Error: Can't Add New State, [" + stateID.ToString() + "] already exists");
            return;
        }
        newState.StateID = stateID;             //Set State's Internal ID To stateID
        _StateCache.Add(stateID, newState);
    }
}

//Base State for all States
//Also acts as a linked list
//
//NOTE:
//  Add PopThrough method to allow quick popping through states?
public class StateBase
{
    System.Enum _StateID;
    public System.Enum StateID { get { return _StateID; } set { _StateID = value; } }

    public StateBase PrevState = null;
    public StateBase NextState = null;

    FSM _FSM;
    public FSM FSMParent { get { return _FSM; } set { _FSM = value; } }

    public StateBase()
    {
        _StateID = StateChangeType.NOT_ASSIGNED;
    }

    //State Added To Finite State Machine
    public virtual void OnEnter()
    { }

    //State Removed From Finite State Machine
    public virtual void OnExit()
    { }

    //Pushed Out Of Focus
    public virtual void OnSuspend()
    { }

    //Popped Bace Into Focus
    public virtual void OnWakeUp()
    { }

    public virtual void OnUpdate()
    { }

    public virtual void OnFixedUpdate()
    { }

    //messaging with the state
    public virtual void OnMessage(System.Enum msgType, params System.Object[] msgData)
    { }
}
