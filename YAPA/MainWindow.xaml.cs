using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Media;
using System.Windows.Shell;
using YAPA.Contracts;
using YAPA.Shared;
using WindowState = System.Windows.WindowState;
using YAPA.YAPA.WPF.Themes.YAPA;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AbstractWindow, INotifyPropertyChanged
    {
        private readonly MotivationalThemeSettings _settings;
        private readonly PomodoroEngineSettings _baseSettings;

        /// <summary>
        /// Pomodoro period enum
        /// </summary>
        public enum PomodoroPeriodType
        {
            Pomodoro,
            ShortBreak,
            LongBreak,
        }

        /// <summary>
        /// Session status
        /// </summary>
        public enum PomodoroPeriodStatus
        {
            Stopped,
            Running,
            Paused,
            Completed,
        }

        /// <summary>
        /// Session action
        /// </summary>
        public enum PomodoroPeriodAction
        {
            NoAction,
            DoStart,
            DoPause,
            DoUnpause,
            DoReset,
            DoComplete,
        }

        private ICommand _showSettings;

        // For INCP
        public event PropertyChangedEventHandler PropertyChanged;


        public MainWindow(MotivationalThemeSettings settings, PomodoroEngineSettings baseSettings)
        {
            _settings = settings;
            _baseSettings = baseSettings;
            InitializeComponent();

            base.DataContext = this;


            // Initialize Pomodoro session
            ResetPomodoroPeriod();
            StorePeriodData(PomodoroPeriodType.Pomodoro, PomodoroPeriodStatus.Stopped);

            // Enable dragging
            base.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        }


        #region Timer

        /// <summary>
        /// Stopwatch instance
        /// </summary>
        private Stopwatch _stopwatch = null;
        private Stopwatch Stopwatch
        {
            get
            {
                if (null == _stopwatch)
                {
                    _stopwatch = new Stopwatch();
                }

                return _stopwatch;
            }
        }

        /// <summary>
        /// Timer instance
        /// </summary>
        private DispatcherTimer _timer = null;
        private DispatcherTimer Timer
        {
            get
            {
                if (null == _timer)
                {
                    _timer = new DispatcherTimer();
                    _timer.Tick += new EventHandler(Timer_Tick);
                    _timer.Interval = new TimeSpan(0, 0, 1);
                }

                return _timer;
            }
        }

        /// <summary>
        /// Timer tick handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!TimerIsRunning)
                return;

            TimeSpan timeElapsed = TimeElapsed;
            int periodTime = 0;
            switch (CurrentPeriodType)
            {
                case PomodoroPeriodType.Pomodoro:
                    periodTime = WorkTime;
                    break;

                case PomodoroPeriodType.ShortBreak:
                    periodTime = BreakTime;
                    break;

                case PomodoroPeriodType.LongBreak:
                    periodTime = LongBreakTime;
                    break;
            }

            if (CountBackwards)
                timeElapsed = TimeSpan.FromMinutes(periodTime) - TimeElapsed;

            CurrentTimeMinutesText = String.Format("{0:00}", timeElapsed.Minutes);
            CurrentTimeSecondsText = String.Format("{0:00}", timeElapsed.Seconds);

            double totalSecondsElapsed = SecondsElapsed;
            int periodSecondsTotal = (periodTime * 60);

            // Update progress bar value
            UpdateProgressValue((double)(totalSecondsElapsed / periodSecondsTotal));

            if (totalSecondsElapsed >= periodSecondsTotal)
                DoCompletePeriod();
        }

        /// <summary>
        /// Updates progress bar value
        /// </summary>
        /// <param name="value"></param>
        private void UpdateProgressValue(double value)
        {
            ProgressValue = value;
        }

        /// <summary>
        /// Starts the timer
        /// </summary>
        private void StartTimer()
        {
            Timer.Start();
            Stopwatch.Start();
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        private void StopTimer()
        {
            Timer.Stop();
            Stopwatch.Reset();
        }

        /// <summary>
        /// Pause the timer
        /// </summary>
        private void PauseTimer()
        {
            Timer.Stop();
            Stopwatch.Stop();
        }

        /// <summary>
        /// Unpause the timer
        /// </summary>
        private void UnpauseTimer()
        {
            StartTimer();
        }

        /// <summary>
        /// Starts the timer
        /// </summary>
        private void RestartTimer()
        {
            Timer.Start();
            Stopwatch.Restart();
        }

        /// <summary>
        /// Checks whether timer is currently running
        /// </summary>
        private bool TimerIsRunning
        {
            get
            {
                return Stopwatch.IsRunning;
            }
        }

        /// <summary>
        /// Current elapsed time
        /// </summary>
        private TimeSpan TimeElapsed
        {
            get
            {
                return Stopwatch.Elapsed;
            }
        }

        /// <summary>
        /// Currently elapsed seconds
        /// </summary>
        private int SecondsElapsed
        {
            get
            {
                return (int)TimeElapsed.TotalSeconds;
            }
        }

        #endregion


        /// <summary>
        /// Current Pomodoro period mode (Pomodoro, Rest, RestLonger)
        /// </summary>
        private PomodoroPeriodType _currentPeriodType = PomodoroPeriodType.Pomodoro;
        private PomodoroPeriodType CurrentPeriodType
        {
            get
            {
                return _currentPeriodType;
            }
        }

        /// <summary>
        /// Pomodoro period status (Stopped, Running, Paused)
        /// </summary>
        private PomodoroPeriodStatus _currentPeriodStatus = PomodoroPeriodStatus.Stopped;
        private PomodoroPeriodStatus CurrentPeriodStatus
        {
            get
            {
                return _currentPeriodStatus;
            }
        }

        /// <summary>
        /// Return resolved pomodoro timer action based on user's intent
        /// </summary>
        private PomodoroPeriodAction ResolvePeriodAction(PomodoroPeriodStatus newPeriodStatus)
        {
            switch (CurrentPeriodStatus)
            {
                case PomodoroPeriodStatus.Stopped:
                    if (newPeriodStatus == PomodoroPeriodStatus.Running)
                        return PomodoroPeriodAction.DoStart;
                    break;

                case PomodoroPeriodStatus.Running:
                    if (newPeriodStatus == PomodoroPeriodStatus.Paused)
                        return PomodoroPeriodAction.DoPause;
                    else if (newPeriodStatus == PomodoroPeriodStatus.Completed)
                        return PomodoroPeriodAction.DoComplete;
                    break;

                case PomodoroPeriodStatus.Paused:
                    if (newPeriodStatus == PomodoroPeriodStatus.Running)
                        return PomodoroPeriodAction.DoUnpause;
                    else if (newPeriodStatus == PomodoroPeriodStatus.Stopped)
                        return PomodoroPeriodAction.DoReset;
                    break;

                case PomodoroPeriodStatus.Completed:
                    if (newPeriodStatus == PomodoroPeriodStatus.Running)
                        return PomodoroPeriodAction.DoStart;
                    else if (newPeriodStatus == PomodoroPeriodStatus.Stopped)
                        return PomodoroPeriodAction.DoReset;
                    break;
            }

            return PomodoroPeriodAction.NoAction;
        }

        /// <summary>
        /// Checks whether period can be switched to defined status
        /// </summary>
        private bool CanSwitchPeriodStatus(PomodoroPeriodStatus newPeriodStatus)
        {
            return (ResolvePeriodAction(newPeriodStatus) != PomodoroPeriodAction.NoAction);
        }

        /// <summary>
        /// Switches the current Pomodoro period
        /// </summary>
        /// <param name="periodType"></param>
        /// <param name="periodStatus"></param>
        private void SwitchPeriod(PomodoroPeriodType periodType, PomodoroPeriodStatus periodStatus)
        {
            // Resolve period action
            PomodoroPeriodAction periodAction = ResolvePeriodAction(periodStatus);

            switch (periodAction)
            {
                case PomodoroPeriodAction.DoStart:
                    if (periodType == PomodoroPeriodType.Pomodoro)
                        DoStartNewPomodoroPeriod();
                    else if (periodType == PomodoroPeriodType.ShortBreak)
                        DoStartShortBreakPeriod();
                    else if (periodType == PomodoroPeriodType.LongBreak)
                        DoStartLongBreakPeriod();
                    break;

                case PomodoroPeriodAction.DoPause:
                    DoPauseCurrentPeriod();
                    break;

                case PomodoroPeriodAction.DoUnpause:
                    DoUnpauseCurrentPeriod();
                    break;

                case PomodoroPeriodAction.DoReset:
                    DoPomodoroReset();
                    break;

                case PomodoroPeriodAction.DoComplete:
                    DoCompletePeriod();
                    break;
            }
        }

        private void DoUnpauseCurrentPeriod()
        {
            if (TimerIsRunning || CurrentPeriodStatus != PomodoroPeriodStatus.Paused)
                return;

            UnpauseTimer();
            StorePeriodData(CurrentPeriodType, PomodoroPeriodStatus.Running);
        }

        private void DoPauseCurrentPeriod()
        {
            if (!TimerIsRunning || CurrentPeriodStatus != PomodoroPeriodStatus.Running)
                return;

            PauseTimer();
            StorePeriodData(CurrentPeriodType, PomodoroPeriodStatus.Paused);
        }

        private void DoPomodoroReset()
        {
            if (TimerIsRunning || (CurrentPeriodStatus != PomodoroPeriodStatus.Paused && CurrentPeriodStatus != PomodoroPeriodStatus.Completed))
                return;

            if (System.Windows.MessageBox.Show(Localizations.General.app_message_confirm_session_reset, Application.Current.MainWindow.Title, MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            if (CurrentPomodoroPeriod > 0)
                HidePeriodCompletedIndicator();

            StopTimer();
            ResetPomodoroPeriod();
            StorePeriodData(PomodoroPeriodType.Pomodoro, PomodoroPeriodStatus.Stopped);
        }

        private PomodoroPeriodType ResolveNextPeriod()
        {
            if (TimerIsRunning)
                return CurrentPeriodType;

            // Start new Pomodoro period
            if (CurrentPeriodStatus == PomodoroPeriodStatus.Stopped)
            {
                return PomodoroPeriodType.Pomodoro;
            }
            else if (CurrentPeriodStatus == PomodoroPeriodStatus.Completed)
            {
                switch (CurrentPeriodType)
                {
                    case PomodoroPeriodType.Pomodoro:
                        if ((CurrentPomodoroPeriod % 4) == 0.0)
                            return PomodoroPeriodType.LongBreak;
                        else
                            return PomodoroPeriodType.ShortBreak;

                    case PomodoroPeriodType.ShortBreak:
                    case PomodoroPeriodType.LongBreak:
                        return PomodoroPeriodType.Pomodoro;
                }
            }

            return CurrentPeriodType;
        }

        private void DoStartNextPeriod()
        {
            if (TimerIsRunning)
                return;

            switch (ResolveNextPeriod())
            {
                case PomodoroPeriodType.Pomodoro:
                    DoStartNewPomodoroPeriod();
                    break;

                case PomodoroPeriodType.ShortBreak:
                    DoStartShortBreakPeriod();
                    break;

                case PomodoroPeriodType.LongBreak:
                    DoStartLongBreakPeriod();
                    break;
            }
        }

        private void DoStartPeriod(PomodoroPeriodType periodType)
        {
            if (TimerIsRunning || (CurrentPeriodStatus != PomodoroPeriodStatus.Stopped && CurrentPeriodStatus != PomodoroPeriodStatus.Completed))
                return;

            if (CurrentPomodoroPeriod > 0)
                HidePeriodCompletedIndicator();

            StartTimer();

            if (periodType == PomodoroPeriodType.Pomodoro)
            {
                CurrentQuote = null; // Reset quote
                AddPomodoroPeriod();
            }

            StorePeriodData(periodType, PomodoroPeriodStatus.Running);
        }

        /// <summary>
        /// Start new Pomodoro period
        /// </summary>
        private void DoStartNewPomodoroPeriod()
        {
            DoStartPeriod(PomodoroPeriodType.Pomodoro);
        }

        /// <summary>
        /// Start new Short break period
        /// </summary>
        private void DoStartShortBreakPeriod()
        {
            DoStartPeriod(PomodoroPeriodType.ShortBreak);
        }

        /// <summary>
        /// Start new Long break period
        /// </summary>
        private void DoStartLongBreakPeriod()
        {
            DoStartPeriod(PomodoroPeriodType.LongBreak);
        }

        private void DoCompletePeriod()
        {
            if (!TimerIsRunning || CurrentPeriodStatus != PomodoroPeriodStatus.Running)
                return;

            if (CurrentPomodoroPeriod > 0)
                ShowPeriodCompletedIndicator();

            StopTimer();

            StorePeriodData(CurrentPeriodType, PomodoroPeriodStatus.Completed);

            // We're done
            UpdateProgressValue(100.0);
            CurrentTimeMinutesText = "00";
            CurrentTimeSecondsText = "00";

            // Mark current Pomodoro as completed
    
        }

        /// <summary>
        /// Current Pomodoro period number
        /// </summary>
        private int _currentPomodoroPeriod = 0;
        public int CurrentPomodoroPeriod
        {
            get
            {
                return _currentPomodoroPeriod;
            }
        }

        /// <summary>
        /// Raise Pomodoro period number
        /// </summary>
        private void AddPomodoroPeriod()
        {
            _currentPomodoroPeriod++;

            // Notify updated current period number
            NotifyPropertyChanged("CurrentPomodoroPeriod");
        }

        /// <summary>
        /// Reset Pomodoro period
        /// </summary>
        private void ResetPomodoroPeriod()
        {
            _currentPomodoroPeriod = 0;

            CurrentTimeMinutesText = "00";
            CurrentTimeSecondsText = "00";
            ProgressValue = .0;

            // Notify updated current period number
            NotifyPropertyChanged("CurrentPomodoroPeriod");
        }

        /// <summary>
        /// Period completed indicator animation instance
        /// </summary>
        private Storyboard _periodCompletedAnimationStoryboard = null;
        private Storyboard PeriodCompletedIndicator
        {
            get
            {
                if (null == _periodCompletedAnimationStoryboard)
                    _periodCompletedAnimationStoryboard = base.TryFindResource("PeriodCompletedIndicatorStoryboard") as Storyboard;

                return _periodCompletedAnimationStoryboard;
            }
            set
            {
                _periodCompletedAnimationStoryboard = value;
            }
        }

        /// <summary>
        /// Show completed indicator
        /// </summary>
        private void ShowPeriodCompletedIndicator()
        {
            if (null != PeriodCompletedIndicator)
                PeriodCompletedIndicator.Begin();
        }

        /// <summary>
        /// Hide completed indicator
        /// </summary>
        private void HidePeriodCompletedIndicator()
        {
            if (null != PeriodCompletedIndicator)
                PeriodCompletedIndicator.Stop();
        }






        /// <summary>
        /// Stores and directly changes current period (it is skipping the business logic)
        /// WARNING: DO NOT CALL THIS ALL ALONE - USE THE "SwitchPeriod()" METHOD INSTEAD
        /// </summary>
        /// <param name="periodType"></param>
        /// <param name="periodStatus"></param>
        private void StorePeriodData(PomodoroPeriodType periodType, PomodoroPeriodStatus periodStatus)
        {
            _currentPeriodType = periodType;
            _currentPeriodStatus = periodStatus;

            NotifyPeriodChanged();
        }

        /// <summary>
        /// Notifies UI about changed period
        /// </summary>
        private void NotifyPeriodChanged()
        {
            NotifyPropertyChanged("CurrentPomodoroPeriod");
            NotifyPropertyChanged("CurrentPeriodText");
            NotifyPropertyChanged("CurrentPeriodTextSource");
            NotifyPropertyChanged("CurrentPeriodIcon");
            NotifyPropertyChanged("ProgressState");
            NotifyPropertyChanged("CanStop");
            NotifyPropertyChanged("CanStart");
            NotifyPropertyChanged("CanPause");
        }

        /// <summary>
        /// Are we using light theme currently?
        /// </summary>
        public bool UseLightTheme
        {
            get
            {
                return (_settings.UseWhiteText);
            }
        }


        #region UI binded properties

        /// <summary>
        /// Main application accent color chosen by user's preference
        /// </summary>
        public SolidColorBrush AccentColor
        {
            get
            {
                return Utils.HexToBrush("#FF0080");
            }
        }

        public double TimerForegroundOpacity
        {
            get
            {
                return 0.3;
            }
        }

        public double TimerShadowOpacity
        {
            get
            {
                return 0.6;
            }
        }

        public SolidColorBrush TimerForegroundColor
        {
            get
            {
                return (UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_TIMER_FOREGROUND) : Utils.HexToBrush(Const.COLOR_DARK_TIMER_FOREGROUND));
            }
        }

        public Color TimerShadowColor
        {
            get
            {
                return (UseLightTheme ? Utils.HexToColor(Const.COLOR_LIGHT_TIMER_SHADOW) : Utils.HexToColor(Const.COLOR_DARK_TIMER_SHADOW));

            }
        }

        public SolidColorBrush WindowBackgroundColor
        {
            get
            {
                return (UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_BACKGROUND) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_BACKGROUND));
            }
        }

        public SolidColorBrush WindowBackground2Color
        {
            get
            {
                return (UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_BACKGROUND2) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_BACKGROUND2));
            }
        }

        public SolidColorBrush WindowForegroundColor
        {
            get
            {
                return (UseLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_FOREGROUND) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_FOREGROUND));
            }
        }

        public Color WindowShadowColor
        {
            get
            {
                return (UseLightTheme ? Utils.HexToColor(Const.COLOR_LIGHT_WINDOW_SHADOW) : Utils.HexToColor(Const.COLOR_DARK_WINDOW_SHADOW));
            }
        }

        public double WindowShadowOpacity
        {
            get
            {
                return (UseLightTheme ? Const.COLOR_LIGHT_WINDOW_SHADOW_OPACITY : Const.COLOR_DARK_WINDOW_SHADOW_OPACITY);
            }
        }

        public bool CanStop
        {
            get
            {
                return CanSwitchPeriodStatus(PomodoroPeriodStatus.Stopped);
            }
        }
        public bool CanStart
        {
            get
            {
                return CanSwitchPeriodStatus(PomodoroPeriodStatus.Running);
            }
        }
        public bool CanPause
        {
            get
            {
                return CanSwitchPeriodStatus(PomodoroPeriodStatus.Paused);
            }
        }

        #endregion

        public ICommand ShowSettings
        {
            get { return _showSettings; }
        }


        public int WorkTime
        {
            get { return _baseSettings.WorkTime; }
        }

        public int BreakTime
        {
            get { return _baseSettings.BreakTime; }
        }

        public int LongBreakTime
        {
            get { return _baseSettings.LongBreakTime; }
        }

        public TaskbarItemProgressState ProgressState
        {
            get {
                switch (CurrentPeriodStatus)
                {
                    case PomodoroPeriodStatus.Completed:
                        return TaskbarItemProgressState.Error;

                    case PomodoroPeriodStatus.Running:
                        return TaskbarItemProgressState.Normal;

                    case PomodoroPeriodStatus.Paused:
                        return TaskbarItemProgressState.Paused;

                    default:
                        return TaskbarItemProgressState.None;
                }
            }
        }

        private double _progressValue = 0.0;
        public double ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                NotifyPropertyChanged("ProgressValue");
            }
        }

        public bool CountBackwards
        {
            get
            {
                return _baseSettings.CountBackwards;
            }
        }

        public string CurrentTimeMinutesText
        {
            set
            {
                CurrentTimeMinutes.Text = value;
                CurrentTimeMinutesInsideWindow.Text = value;
            }
        }

        public string CurrentTimeSecondsText
        {
            set
            {
                CurrentTimeSeconds.Text = value;
                CurrentTimeSecondsInsideWindow.Text = value;
            }
        }

        /// <summary>
        /// Current quote text
        /// </summary>
        private Quote _currentQuote = null;
        public Quote CurrentQuote
        {
            get
            {
                if (null == _currentQuote)
                {
                    _currentQuote = Quotes.GetRandomQuote();
                }

                return _currentQuote;
            }
            set
            {
                _currentQuote = value;
            }
        }

        /// <summary>
        /// Current period motivation text
        /// </summary>
        public string CurrentPeriodText
        {
            get
            {
                switch (CurrentPeriodType)
                {
                    case PomodoroPeriodType.Pomodoro:
                        if (CurrentPeriodStatus == PomodoroPeriodStatus.Running || CurrentPeriodStatus == PomodoroPeriodStatus.Paused)
                            return (!string.IsNullOrWhiteSpace(CurrentQuote.Text) ? CurrentQuote.Text : Localizations.General.app_period_pomodoro_caption_default);
                        else if (CurrentPeriodStatus == PomodoroPeriodStatus.Stopped)
                            return Localizations.General.app_period_motivation_start_pomodoro;
                        else
                        {
                            PomodoroPeriodType nextPeriodType = ResolveNextPeriod();
                            if (nextPeriodType == PomodoroPeriodType.ShortBreak)
                                return String.Format(Localizations.General.app_period_motivation_start_short_break, BreakTime);
                            else if (nextPeriodType == PomodoroPeriodType.LongBreak)
                                return String.Format(Localizations.General.app_period_motivation_start_long_break, LongBreakTime);
                            else
                                return Localizations.General.app_period_press_play_to_start;
                        }

                    case PomodoroPeriodType.ShortBreak:
                        if (CurrentPeriodStatus == PomodoroPeriodStatus.Completed)
                            return Localizations.General.app_period_motivation_start_pomodoro;
                        else
                            return String.Format(Localizations.General.app_period_short_break_caption, BreakTime);

                    case PomodoroPeriodType.LongBreak:
                        if (CurrentPeriodStatus == PomodoroPeriodStatus.Completed)
                            return Localizations.General.app_period_motivation_start_pomodoro;
                        else
                            return String.Format(Localizations.General.app_period_long_break_caption, LongBreakTime);

                    default:
                        return Localizations.General.app_period_motivation_start_pomodoro;
                }
            }
        }

        /// <summary>
        /// Motivation text source
        /// </summary>
        public string CurrentPeriodTextSource
        {
            get
            {
                if (CurrentPeriodType == PomodoroPeriodType.Pomodoro && (CurrentPeriodStatus == PomodoroPeriodStatus.Running || CurrentPeriodStatus == PomodoroPeriodStatus.Paused))
                    return (!string.IsNullOrWhiteSpace(CurrentQuote.Source) ? CurrentQuote.Source : string.Empty);

                return string.Empty;
            }
        }

        /// <summary>
        /// Period icon
        /// </summary>
        public string CurrentPeriodIcon
        {
            get
            {
                switch (CurrentPeriodType)
                {
                    case PomodoroPeriodType.Pomodoro:
                        return Const.ICON_PERIOD_POMODORO;

                    case PomodoroPeriodType.ShortBreak:
                        return Const.ICON_PERIOD_REST;

                    case PomodoroPeriodType.LongBreak:
                        return Const.ICON_PERIOD_REST_LONG;

                    default:
                        return Const.ICON_PERIOD_STOPPED;
                }
            }
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            base.DragMove();
        }

        /// <summary>
        /// Used to raise change notifications to other consumers.
        /// Needed for INotifyProperty
        /// </summary>
        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.ShowSettings.Execute(this);
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            if (TimerIsRunning)
            {
                if (System.Windows.MessageBox.Show(Localizations.General.app_message_confirm_exit, Application.Current.MainWindow.Title, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }

            base.Close();
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            base.WindowState = WindowState.Minimized;
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            this.ShowSettings.Execute(this);
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPeriodStatus == PomodoroPeriodStatus.Paused)
                DoUnpauseCurrentPeriod();
            else
                DoStartNextPeriod();
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            DoPomodoroReset();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            DoPauseCurrentPeriod();
        }
    }
}
