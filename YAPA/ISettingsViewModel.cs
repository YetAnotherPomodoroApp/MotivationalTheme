using System.Windows.Input;

namespace YAPA
{
    /// <summary>
    /// View model used for settings UI.
    /// </summary>
    public interface ISettingsViewModel
    {
        /// <summary>
        /// Closes this view.
        /// </summary>
        void CloseSettings();

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        double TimerForegroundOpacity { get; set; }

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        double TimerShadowOpacity { get; set; }

        /// <summary>
        /// True if we are to use light theme;
        /// otherwise, false.
        /// </summary>
        bool UseLightTheme { get; set; }

        /// <summary>
        /// The ammount of minutes
        /// </summary>
        int WorkTime { get; set; }

        /// <summary>
        /// The ammount of minutes
        /// </summary>
        int BreakTime { get; set; }

        /// <summary>
        /// The ammount of minutes
        /// </summary>
        int LongBreakTime { get; set; }

        /// <summary>
        /// Use sounds
        /// </summary>
        bool UseSoundEfects { get; set; }

        /// <summary>
        /// Count time backwards
        /// </summary>
        bool CountBackwards { get; set; }

        /// <summary>
        /// Command invoked when user clicks 'Done'
        /// </summary>
        ICommand SaveSettings { get; }
    }
}
