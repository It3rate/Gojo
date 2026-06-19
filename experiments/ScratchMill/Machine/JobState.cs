namespace ScratchMill.Machine
{
    public enum JobState
    {
        Idle = 0,
        Header,

        TopSide,
        TopZeroXY,
        TopProbe,
        TopEdgeProbe,
        TopMoveTo0_0,
        TopAutolevel,
        TopTracks,
        TopContinuity,
        TopCleanup,
        TopRecut,
        TopApplySolderMask,
        TopMaskRemoval,
        TopGraphics,
        TopEdgeScratches,
        TopDrills,
        TopRouterCuts,
        TopSideComplete,

        BottomSide,
        BottomZeroXY,
        BottomProbe,
        BottomEdgeProbe,
        BottomMoveTo0_0,
        BottomAutolevel,
        BottomTracks,
        BottomContinuity,
        BottomCleanup,
        BottomRecut,
        BottomApplySolderMask,
        BottomMaskRemoval,
        BottomGraphics,
        BottomEdgeScratches, // edge cut with scratch bit, otherwise after drills
        BottomDrills,
        BottomRouterCuts,
        BottomSideComplete,

        Complete,
        End,
        Error,
    }
    static class JobStateExtensions
    {
        public static BoardSide GetSide(this JobState state) =>
            state.IsBottomLayer() ? BoardSide.Back : BoardSide.Front;
        public static bool IsTopLayer(this JobState state) => state >= JobState.TopSide && state < JobState.BottomSide;
        public static bool IsBottomLayer(this JobState state) => state >= JobState.BottomSide;
        public static bool IsTrackLayer(this JobState state)
        {
            var result = false;
            switch (state)
            {
                case JobState.TopTracks:
                case JobState.TopCleanup:
                case JobState.TopRecut:
                case JobState.TopMaskRemoval:
                case JobState.BottomTracks:
                case JobState.BottomCleanup:
                case JobState.BottomRecut:
                case JobState.BottomMaskRemoval:
                    result = true;
                    break;
            }
            return result;
        }
        public static bool IsMillLayer(this JobState state)
        {
            var result = false;
            switch (state)
            {
                case JobState.TopTracks:
                case JobState.TopCleanup:
                case JobState.TopRecut:
                case JobState.TopMaskRemoval:
                case JobState.TopGraphics:
                case JobState.TopDrills:
                case JobState.TopEdgeScratches:
                case JobState.BottomTracks:
                case JobState.BottomCleanup:
                case JobState.BottomRecut:
                case JobState.BottomMaskRemoval:
                case JobState.BottomGraphics:
                case JobState.BottomDrills:
                case JobState.BottomEdgeScratches:
                    result = true;
                    break;
            }
            return result;
        }
        public static JobState GetBottomVersion(this JobState state)
        {
            var result = state;
            if (state.IsTopLayer())
            {
                result = state + (int)JobState.BottomSide - (int)JobState.TopSide;
            }
            return result;
        }
        public static JobState GetTopVersion(this JobState state)
        {
            var result = state;
            if (state.IsBottomLayer())
            {
                result = state - (int)JobState.BottomSide - (int)JobState.TopSide;
            }
            return result;
        }
        public static JobState GetVersionForLayer(this JobState state, bool isTop) => (isTop) ? GetTopVersion(state) : GetBottomVersion(state);
        public static bool IsProbeState(this JobState state) => state == JobState.TopProbe || state == JobState.BottomProbe;
        public static bool IsEdgeProbeState(this JobState state) => state == JobState.TopEdgeProbe || state == JobState.BottomEdgeProbe;
        public static bool IsAutolevelState(this JobState state) => state == JobState.TopAutolevel || state == JobState.BottomAutolevel;
        public static bool IsTracksState(this JobState state) => state == JobState.TopTracks || state == JobState.BottomTracks;
        public static bool IsCleanupState(this JobState state) => state == JobState.TopCleanup || state == JobState.BottomCleanup;
        public static bool IsRecutState(this JobState state) =>
            state == JobState.TopRecut || state == JobState.BottomRecut ||
            state == JobState.TopContinuity || state == JobState.BottomContinuity;
        public static bool IsSolderMaskState(this JobState state) =>
            state == JobState.TopApplySolderMask || state == JobState.BottomApplySolderMask ||
            state == JobState.TopMaskRemoval || state == JobState.BottomMaskRemoval;
        public static bool IsGraphicsState(this JobState state) => state == JobState.TopGraphics || state == JobState.BottomGraphics;
        public static bool IsDrillsState(this JobState state) => state == JobState.TopDrills || state == JobState.BottomDrills;
        public static bool IsEdgeScratchesState(this JobState state) => state == JobState.TopEdgeScratches || state == JobState.BottomEdgeScratches;

    }
}
