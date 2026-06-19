namespace ScratchMill.GCode
{
    public enum PassKind
    {
        TestPattern = 'X',

        Track = 'T',
        BurrRemoval = 'B',
        Recut = 'R',
        SolderMask = 'M',
        SolderPaste = 'P',
        Graphic = 'G',
        Drill = 'D',
        Via = 'V',
        EdgeCut = 'C',
        RouterCut = 'X',
    }
}
