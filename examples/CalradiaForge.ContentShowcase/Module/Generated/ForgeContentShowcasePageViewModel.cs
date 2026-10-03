using CalradiaForge.Sdk;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradiaForge.ContentShowcase
{
    [ForgeUiPage("calradiaforge.content.showcase", "ForgeContentShowcase", "cfcs_title", Context = Context.Any)]
    public sealed class ForgeContentShowcasePageViewModel : ViewModel
    {
        [DataSourceProperty]
        public string Title => new TextObject("{=cfcs_title}Calradia Forge content showcase").ToString();

        [DataSourceProperty]
        public string Body => new TextObject("{=cfcs_body}This read-only page demonstrates a module-owned Gauntlet ViewModel and static troop and item XML. It does not run tests or change game state.").ToString();

        [DataSourceProperty]
        public string CloseLabel => new TextObject("{=cfcs_close}Close").ToString();

        [ForgeUiCommand("close", "ExecuteClose", Context = Context.Any, ChangesState = false)]
        public void ExecuteClose() => ForgeUI.ClosePage();
    }
}