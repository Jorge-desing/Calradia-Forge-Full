namespace CalradiaForge.Mod
{
    public struct ToolDefinition
    {
        public string Tag { get; }
        public string Title { get; }
        public string Hint { get; }

        public ToolDefinition(string tag, string title, string hint)
        {
            Tag = tag;
            Title = title;
            Hint = hint;
        }
    }
}
