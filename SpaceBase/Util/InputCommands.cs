namespace Space.Util
{
    /// <summary>
    /// Possible command type in the game (ship control essentially).
    /// </summary>
    public enum GameCommand
    {
        /// <summary>
        /// Invalid/no command.
        /// </summary>
        None,

        #region Movement / Camera

        /// <summary>
        /// Accelerate up.
        /// </summary>
        Up,

        /// <summary>
        /// Accelerate down.
        /// </summary>
        Down,

        /// <summary>
        /// Accelerate left.
        /// </summary>
        Left,

        /// <summary>
        /// Accelerate right.
        /// </summary>
        Right,

        /// <summary>
        /// Stabilize the ship's position.
        /// </summary>
        Stabilize,

        /// <summary>
        /// Zoom the camera in.
        /// </summary>
        ZoomIn,

        /// <summary>
        /// Zoom the camera out.
        /// </summary>
        ZoomOut,

        #endregion

        #region Fighting / World interaction

        /// <summary>
        /// Fire all weapons.
        /// </summary>
        Shoot,

        /// <summary>
        /// Activate the shield.
        /// </summary>
        Shield,

        /// <summary>
        /// Pick up nearby items.
        /// </summary>
        PickUp,

        /// <summary>
        /// Use object in game (e.g. space station for trading).
        /// </summary>
        Use,

        #endregion

        #region GUI
        
        /// <summary>
        /// Go back in the menu, or abort editing (text fields).
        /// </summary>
        Back,

        /// <summary>
        /// Open the ingame menu.
        /// </summary>
        Menu,

        /// <summary>
        /// Open the inventory.
        /// </summary>
        Inventory,

        /// <summary>
        /// Open the character sheet.
        /// </summary>
        Character,

        /// <summary>
        /// Toggle graph with debug info rendering.
        /// </summary>
        ToggleGraphs,

        /// <summary>
        /// Open up the console.
        /// </summary>
        Console,

        #endregion

        #region Targeting

        /// <summary>
        /// Un-targets whatever we're currently targeting.
        /// </summary>
        ClearTarget,

        /// <summary>
        /// Select the next target (further away from our ship than the
        /// current target).
        /// </summary>
        NextTarget,

        /// <summary>
        /// Select the previous target (closer to our ship than the current
        /// target).
        /// </summary>
        PreviousTarget,

        /// <summary>
        /// Select the next enemy target (further away from our ship than
        /// our current target, closest one if the current target is
        /// friendly).
        /// </summary>
        NextEnemyTarget,

        /// <summary>
        /// Select the previous enemy target (closer to our ship than
        /// our current target, furthest one if the current target is
        /// friendly).
        /// </summary>
        PreviousEnemyTarget,

        /// <summary>
        /// Select the next friendly target (further away from our ship than
        /// our current target, closest one if the current target is
        /// an enemy).
        /// </summary>
        NextFriendlyTarget,

        /// <summary>
        /// Select the previous enemy target (closer to our ship than
        /// our current target, furthest one if the current target is
        /// an enemy).
        /// </summary>
        PreviousFriendlyTarget,

        /// <summary>
        /// Target the element closest to the current cursor position.
        /// </summary>
        CursorTarget

        #endregion
    }

    public enum GamePadCommand
    {
        /// <summary>
        /// Invalid/no command.
        /// </summary>
        None,

        /// <summary>
        /// Horizontal acceleration axis.
        /// </summary>
        AccelerateX,

        /// <summary>
        /// Vertical acceleration axis.
        /// </summary>
        AccelerateY,

        /// <summary>
        /// Horizontal look axis.
        /// </summary>
        LookX,

        /// <summary>
        /// Vertical look axis.
        /// </summary>
        LookY
    }
}
