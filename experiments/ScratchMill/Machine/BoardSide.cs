namespace ScratchMill.Machine
{
    public enum BoardSide
    {
        None,
        Front,
        Back,
        Test,
    }
    static class BoardSideExtensions
    {
        public static string Prefix(this BoardSide side)
        {
            return side switch
            {
                BoardSide.Front => "F",
                BoardSide.Back => "B",
                BoardSide.Test => "T",
                _ => "None"
            };
        }
        public static bool IsTop(this BoardSide side) => side == BoardSide.Front;
        public static bool IsNotTop(this BoardSide side) => side != BoardSide.Front;
        public static bool IsBottom(this BoardSide side) => side == BoardSide.Back;
        public static bool IsNotBottom(this BoardSide side) => side != BoardSide.Back;
        public static bool IsTemporary(this BoardSide side) => side != BoardSide.Front && side != BoardSide.Back;
    }
}
