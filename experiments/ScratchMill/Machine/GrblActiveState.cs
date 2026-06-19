namespace ScratchMill.Machine
{
    public enum GrblActiveState
    {
        Unknown,
        Idle,
        Run,
        Jog,
        Hold,
        Alarm,
        Check,
        Door,
        Tool,
        Home,
        Sleep
    }
    static class GrblMachineStateExtensions
    {

        public static GrblActiveState GrblMachineStateFromString(this string stateName)
        {
            var result = GrblActiveState.Unknown;
            stateName = stateName.ToUpperInvariant();
            switch (stateName)
            {
                case "IDLE":
                    result = GrblActiveState.Idle;
                    break;
                case "RUN":
                    result = GrblActiveState.Run;
                    break;
                case "JOG":
                    result = GrblActiveState.Jog;
                    break;
                case "HOLD":
                    result = GrblActiveState.Hold;
                    break;
                case "ALARM":
                    result = GrblActiveState.Alarm;
                    break;
                case "Check":
                    result = GrblActiveState.Check;
                    break;
                case "Door":
                    result = GrblActiveState.Door;
                    break;
                case "Tool":
                    result = GrblActiveState.Tool;
                    break;
                case "Home":
                    result = GrblActiveState.Home;
                    break;
                case "Sleep":
                    result = GrblActiveState.Sleep;
                    break;
            }
            return result;
        }
    }
}
