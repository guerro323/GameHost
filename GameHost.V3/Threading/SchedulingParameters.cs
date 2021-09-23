namespace GameHost.V3.Threading
{
    public struct SchedulingParameters
    {
        public bool Once;

        public static readonly SchedulingParameters AsOnce = new() {Once = true};
    }

    public struct SchedulingParametersWithArgs
    {
        public bool OnceWithMethod;
        public bool OnceWithMethodAndArgs;

        public static readonly SchedulingParametersWithArgs AsOnce = new() {OnceWithMethod = true};
        public static readonly SchedulingParametersWithArgs AsOnceWithArgs = new() {OnceWithMethodAndArgs = true};
    }
}