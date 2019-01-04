namespace Clipster
{
    public class PreviewAttribute
    {
        public string Source { get; set; }
        public string Output { get; set; }
        public int BeginSeconds { get; set; }
        public int SnippetLengthInSeconds { get; set; }
        public int DesiredSnippets { get; set; }
        public int VideoHeightInPixels { get; set; }
        public int VideoWidthInPixels { get; set; }
        public bool Verbose { get; set; }
    }
}
