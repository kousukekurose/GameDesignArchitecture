using UnityEngine;

namespace GameDesignArchitecture.Events
{
    public readonly struct GameStateChangedEvent
    {
        public IGameManagerState NewState {get;}
        public GameStateChangedEvent(IGameManagerState newState) => NewState = newState;
    }

    public readonly struct PlayerGeneratEvent{}

}