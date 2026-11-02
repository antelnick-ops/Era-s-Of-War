namespace ErasOfWar.Simulation.Base
{
    public enum BaseError
    {
        None = 0,
        UnknownType,
        NotFound,
        LockedByCc,
        CountLimitReached,
        OutOfBounds,
        Overlap,
        MaxLevel,
        LevelNeedsCc,
        CannotRemoveCommandCenter,
    }

    /// <summary>Outcome of a base request. <see cref="Message"/> is plain language, safe to show the player.</summary>
    public sealed class BaseResult
    {
        public bool Ok => Error == BaseError.None;
        public BaseError Error { get; }
        public string Message { get; }
        /// <summary>The building placed / moved / upgraded / removed (0 if none).</summary>
        public int BuildingId { get; }
        /// <summary>For <see cref="BaseError.LockedByCc"/> and <see cref="BaseError.LevelNeedsCc"/>: the CC level needed.</summary>
        public int RequiredCc { get; }

        private BaseResult(BaseError error, string message, int buildingId, int requiredCc)
        {
            Error = error;
            Message = message;
            BuildingId = buildingId;
            RequiredCc = requiredCc;
        }

        public static BaseResult Success(int buildingId, string message = "") =>
            new BaseResult(BaseError.None, message, buildingId, 0);

        public static BaseResult Fail(BaseError error, string message, int buildingId = 0, int requiredCc = 0) =>
            new BaseResult(error, message, buildingId, requiredCc);

        public override string ToString() => Ok ? $"OK {Message}" : $"{Error}: {Message}";
    }
}
