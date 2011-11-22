namespace Engine.Commands
{
    enum InternalCommandType
    {
        Ack,
        GameQuery,
        GameInfo,

        AckAllFollowing,

        Join,
        JoinResponse,
        Leave
    }

    public enum CommandType
    {
        PlayerJoined = InternalCommandType.Leave + 1,
        PlayerLeft,
        GameStateQuery,
        GameState,

        LastEngineCommand = 50
    }
}
