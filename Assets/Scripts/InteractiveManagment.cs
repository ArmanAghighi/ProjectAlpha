public enum InteractionState
{
    None,
    Ready,
    OnZoom,
    TurningPage,
    TrackingLost,
    WaitingForReset
}

public static class InteractiveManagment
{
    public static InteractionState CurrentState = InteractionState.None;

    public static void SetInteractionState(InteractionState state) 
    {
        if (CurrentState == state) 
            return;
        
        CurrentState = state; 
    }
}
