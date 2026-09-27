using System;
using TaleWorlds.Library;

namespace CalradiaForge.Mod
{
    public class ToolItemVM : ViewModel
    {
        private readonly Action<ToolItemVM> _onSelect;
        private bool _isSelected;
        private bool _isVisible = true;

        public string Tag { get; }

        [DataSourceProperty]
        public string Title { get; }

        [DataSourceProperty]
        public string Hint { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsVisible));
                }
            }
        }

        public ToolItemVM(string tag, string title, string hint, Action<ToolItemVM> onSelect)
        {
            Tag = tag;
            Title = title;
            Hint = hint;
            _onSelect = onSelect;
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }
    }
}
