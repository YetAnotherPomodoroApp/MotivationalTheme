using System.Windows.Input;
using System.Windows.Media;

namespace YAPA
{
    /// <summary>
    /// View model definition for the main clock.
    /// </summary>
    public interface IMainViewModel
    {
        /// <summary>
        /// Command binding used to show settings dialog
        /// </summary>
        ICommand ShowSettings { get; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        int WorkTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        int BreakTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        int LongBreakTime { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        bool UseSoundEffects { get; set; }

        /// <summary>
        /// Count time backwards
        /// </summary>
        bool CountBackwards { get; set; }

        /// <summary>
        /// The font size used to render the clock.
        /// </summary>
        bool UseLightTheme { get; set; }

        /// <summary>
        /// The desired opacity of the clock;
        /// </summary>
        double TimerForegroundOpacity { get; set; }

        /// <summary>
        /// The desired opacity of the shadow;
        /// </summary>
        double TimerShadowOpacity { get; set; }

        /// <summary>
        /// The color used to render the clock.
        /// </summary>
        SolidColorBrush TimerForegroundColor { get; set; }

        /// <summary>
        /// The color used to render the shadow.
        /// </summary>
        Color TimerShadowColor { get; set; }

        /// <summary>
        /// Accent color selected by user
        /// </summary>
        SolidColorBrush AccentColor { get; set; }

        /// <summary>
        /// Color used to generate window background
        /// </summary>
        SolidColorBrush WindowBackgroundColor { get; set; }

        /// <summary>
        /// Color used to differentiate background inside the window
        /// </summary>
        SolidColorBrush WindowBackground2Color { get; set; }

        /// <summary>
        /// Color used to render fonts inside the window
        /// </summary>
        SolidColorBrush WindowForegroundColor { get; set; }

        /// <summary>
        /// Color used to render window's shadow
        /// </summary>
        Color WindowShadowColor { get; set; }

        /// <summary>
        /// Opacity of the window's shadow
        /// </summary>
        double WindowShadowOpacity { get; set; }
    }
}
