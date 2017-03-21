using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAPA.Contracts;

namespace YAPA
{
using System;

namespace YAPA.WPF.Themes.YAPA
{

    public class YapaThemeMeta : IThemeMeta
    {
        public string Title => "YAPA 1.0";

        public Type Theme => typeof(MainWindow);

        public Type Settings => typeof(MotivationalThemeSettings);

        public Type SettingEditWindow => null;
    }

    public class MotivationalThemeSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public int Width
        {
            get { return _settings.Get(nameof(Width), 200); }
            set { _settings.Update(nameof(Width), value); }
        }

        public double ClockOpacity
        {
            get { return _settings.Get(nameof(ClockOpacity), 0.6); }
            set { _settings.Update(nameof(ClockOpacity), value); }
        }

        public double ShadowOpacity
        {
            get { return _settings.Get(nameof(ShadowOpacity), 0.6); }
            set { _settings.Update(nameof(ShadowOpacity), value); }
        }

        public bool UseWhiteText
        {
            get { return _settings.Get(nameof(UseWhiteText), false); }
            set { _settings.Update(nameof(UseWhiteText), value); }
        }

        public string TextBrush
        {
            get { return _settings.Get(nameof(TextBrush), "White"); }
            set { _settings.Update(nameof(TextBrush), value); }
        }

        public bool DisableFlashingAnimation
        {
            get { return _settings.Get(nameof(DisableFlashingAnimation), false); }
            set { _settings.Update(nameof(DisableFlashingAnimation), value); }
        }


        public MotivationalThemeSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent("MotivationalTheme");
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}

}
