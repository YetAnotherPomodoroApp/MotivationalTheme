﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Media;
using System.Windows.Shell;
using System.Reflection;
using WindowState = System.Windows.WindowState;
using GDIScreen = System.Windows.Forms.Screen;
using System.Windows.Interop;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainViewModel, INotifyPropertyChanged
    {
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


        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            _showSettings = new ShowSettings(this);

            // Initialize Pomodoro session
            ResetPomodoroPeriod();
            StorePeriodData(PomodoroPeriodType.Pomodoro, PomodoroPeriodStatus.Stopped);

            // Enable dragging
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            // Save window position on close
            this.Closing += MainWindow_Closing;

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }


        private ItemRepository _repository = null;
        private ItemRepository Repository
        {
            get
            {
                if (null == _repository)
                    _repository = new ItemRepository();

                return _repository;
            }
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

            PlaySoundPeriodStart();
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

            PlaySoundPeriodCompleted();
            StorePeriodData(CurrentPeriodType, PomodoroPeriodStatus.Completed);

            // We're done
            UpdateProgressValue(100.0);
            CurrentTimeMinutesText = "00";
            CurrentTimeSecondsText = "00";

            // Mark current Pomodoro as completed
            if (CurrentPeriodType == PomodoroPeriodType.Pomodoro)
                Repository.CompletePomodoro();
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
                    _periodCompletedAnimationStoryboard = TryFindResource("PeriodCompletedIndicatorStoryboard") as Storyboard;

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
        /// Sound instance of period start
        /// @TODO: PLAY SOUNDS ON BACKGROUND THREAD SINCE THIS WAY IT BLOCKS UI
        /// </summary>
        private SoundPlayer _soundPeriodStart = null;
        private SoundPlayer SoundPeriodStart
        {
            get
            {
                if (!UseSoundEffects)
                    return null;

                if (null == _soundPeriodStart)
                    _soundPeriodStart = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + @"\Resources\tick.wav");

                return _soundPeriodStart;
            }
            set
            {
                _soundPeriodStart = value;
            }
        }

        /// <summary>
        /// Sound instance of period complete
        /// @TODO: PLAY SOUNDS ON BACKGROUND THREAD SINCE THIS WAY IT BLOCKS UI
        /// </summary>
        private SoundPlayer _soundPeriodCompleted = null;
        private SoundPlayer SoundPeriodCompleted
        {
            get
            {
                if (!UseSoundEffects)
                    return null;

                if (null == _soundPeriodCompleted)
                    _soundPeriodCompleted = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + @"\Resources\ding.wav");

                return _soundPeriodCompleted;
            }
            set
            {
                _soundPeriodCompleted = value;
            }
        }

        /// <summary>
        /// Plays period start sound if UseSoundEffects flag is allowed 
        /// </summary>
        private void PlaySoundPeriodStart()
        {
            if (null != SoundPeriodStart)
                SoundPeriodStart.Play();
        }

        /// <summary>
        /// Plays period completed sound if UseSoundEffects flag is allowed 
        /// </summary>
        private void PlaySoundPeriodCompleted()
        {
            if (null != SoundPeriodCompleted)
                SoundPeriodCompleted.Play();
        }

        /// <summary>
        /// Stops all sounds
        /// </summary>
        private void StopAllSounds()
        {
            if (null != SoundPeriodStart)
                SoundPeriodStart.Stop();

            if (null != SoundPeriodCompleted)
                SoundPeriodCompleted.Stop();
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
                return (YAPA.Properties.Settings.Default.UseLightTheme);
            }
            set
            {
                YAPA.Properties.Settings.Default.UseLightTheme = value;
                NotifyPropertyChanged("UseLightTheme");
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
                return Utils.HexToBrush(YAPA.Properties.Settings.Default.AccentColor);
            }
            set
            {
                YAPA.Properties.Settings.Default.AccentColor = Utils.BrushToHex(value);
                NotifyPropertyChanged("AccentColor");
            }
        }

        public double TimerForegroundOpacity
        {
            get
            {
                return YAPA.Properties.Settings.Default.TimerForegroundOpacity;
            }
            set
            {
                YAPA.Properties.Settings.Default.TimerForegroundOpacity = value;
                NotifyPropertyChanged("TimerForegroundOpacity");
            }
        }

        public double TimerShadowOpacity
        {
            get
            {
                return YAPA.Properties.Settings.Default.TimerShadowOpacity;
            }
            set
            {
                YAPA.Properties.Settings.Default.TimerShadowOpacity = value;
                NotifyPropertyChanged("TimerShadowOpacity");
            }
        }

        public SolidColorBrush TimerForegroundColor
        {
            get
            {
                return Utils.HexToBrush(YAPA.Properties.Settings.Default.TimerForegroundColor);
            }
            set
            {
                YAPA.Properties.Settings.Default.TimerForegroundColor = Utils.BrushToHex(value);
                NotifyPropertyChanged("TimerForegroundColor");
            }
        }

        public Color TimerShadowColor
        {
            get
            {
                return Utils.HexToColor(YAPA.Properties.Settings.Default.TimerShadowColor);
            }
            set
            {
                YAPA.Properties.Settings.Default.TimerShadowColor = Utils.ColorToHex(value);
                NotifyPropertyChanged("TimerShadowColor");
            }
        }

        public SolidColorBrush WindowBackgroundColor
        {
            get
            {
                return Utils.HexToBrush(YAPA.Properties.Settings.Default.WindowBackgroundColor);
            }
            set
            {
                YAPA.Properties.Settings.Default.WindowBackgroundColor = Utils.BrushToHex(value);
                NotifyPropertyChanged("WindowBackgroundColor");
            }
        }

        public SolidColorBrush WindowBackground2Color
        {
            get
            {
                return Utils.HexToBrush(YAPA.Properties.Settings.Default.WindowBackground2Color);
            }
            set
            {
                YAPA.Properties.Settings.Default.WindowBackground2Color = Utils.BrushToHex(value);
                NotifyPropertyChanged("WindowBackground2Color");
            }
        }

        public SolidColorBrush WindowForegroundColor
        {
            get
            {
                return Utils.HexToBrush(YAPA.Properties.Settings.Default.WindowForegroundColor);
            }
            set
            {
                YAPA.Properties.Settings.Default.WindowForegroundColor = Utils.BrushToHex(value);
                NotifyPropertyChanged("WindowForegroundColor");
            }
        }

        public Color WindowShadowColor
        {
            get
            {
                return Utils.HexToColor(YAPA.Properties.Settings.Default.WindowShadowColor);
            }
            set
            {
                YAPA.Properties.Settings.Default.WindowShadowColor = Utils.ColorToHex(value);
                NotifyPropertyChanged("WindowShadowColor");
            }
        }

        public double WindowShadowOpacity
        {
            get
            {
                return YAPA.Properties.Settings.Default.WindowShadowOpacity;
            }
            set
            {
                YAPA.Properties.Settings.Default.WindowShadowOpacity = value;
                NotifyPropertyChanged("WindowShadowOpacity");
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


        #region OS interactions

        /// <summary>
        /// Save window position when application exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (YAPA.Properties.Settings.Default.IsFirstRun)
            {
                YAPA.Properties.Settings.Default.IsFirstRun = false;
            }

            GDIScreen currentScreen = GDIScreen.FromHandle(new WindowInteropHelper(this).Handle);

            YAPA.Properties.Settings.Default.CurrentScreenHeight = currentScreen.WorkingArea.Height;
            YAPA.Properties.Settings.Default.CurrentScreenWidth = currentScreen.WorkingArea.Width;

            YAPA.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handle window position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateJumpList();

            //if you want to handle to command line args on the first instance you may want it to go here
            //or in the app.xaml.cs
            //ProcessCommandLineArgs(SingleInstance<App>.CommandLineArgs);

            GDIScreen currentScreen = GDIScreen.FromHandle(new WindowInteropHelper(this).Handle);

            bool screenChanged = (currentScreen.WorkingArea.Height != YAPA.Properties.Settings.Default.CurrentScreenHeight ||
                                currentScreen.WorkingArea.Width != YAPA.Properties.Settings.Default.CurrentScreenWidth);

            // default position only for first run or when screen size changes
            // position the clock at top / right, primary screen
            if (YAPA.Properties.Settings.Default.IsFirstRun || screenChanged)
            {
                this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 15.0;
                this.Top = 0;
            }
        }

        /// <summary>
        /// OS integrated jumplist
        /// </summary>
        private void CreateJumpList()
        {
            JumpList jumpList = new JumpList();
            JumpList.SetJumpList(Application.Current, jumpList);

            JumpTask startTask = new JumpTask();
            startTask.Title = Localizations.General.app_jumplist_item_start;
            startTask.Description = Localizations.General.app_jumplist_item_start_title;
            startTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            startTask.Arguments = "/start";
            startTask.IconResourceIndex = 7;
            jumpList.JumpItems.Add(startTask);

            JumpTask pauseTask = new JumpTask();
            pauseTask.Title = Localizations.General.app_jumplist_item_pause;
            pauseTask.Description = Localizations.General.app_jumplist_item_pause_title;
            pauseTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            pauseTask.Arguments = "/pause";
            pauseTask.IconResourceIndex = 3;
            jumpList.JumpItems.Add(pauseTask);

            JumpTask stopTask = new JumpTask();
            stopTask.Title = Localizations.General.app_jumplist_item_restart;
            stopTask.Description = Localizations.General.app_jumplist_item_restart_title;
            stopTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            stopTask.Arguments = "/restart";
            stopTask.IconResourceIndex = 4;
            jumpList.JumpItems.Add(stopTask);

            JumpTask resetTask = new JumpTask();
            resetTask.Title = Localizations.General.app_jumplist_item_reset;
            resetTask.Description = Localizations.General.app_jumplist_item_reset_title;
            resetTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            resetTask.Arguments = "/reset";
            resetTask.IconResourceIndex = 2;
            jumpList.JumpItems.Add(resetTask);

            JumpTask settingsTask = new JumpTask();
            settingsTask.Title = Localizations.General.app_jumplist_item_settings;
            settingsTask.Description = Localizations.General.app_jumplist_item_settings_title;
            settingsTask.ApplicationPath = Assembly.GetEntryAssembly().Location;
            settingsTask.Arguments = "/settings";
            settingsTask.IconResourceIndex = 5;
            jumpList.JumpItems.Add(settingsTask);

            jumpList.Apply();
        }

        /// <summary>
        /// Process command line parameters to support OS integrated jumplist
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool ProcessCommandLineArgs(IList<string> args)
        {
            if (args == null || args.Count == 0)
                return true;

            //if ((args.Count > 1))
            //{
            //    //the first index always contains the location of the exe so we need to check the second index
            //    if ((args[1].ToLowerInvariant() == "/start"))
            //    {
            //        if (!_stopWatch.IsRunning)
            //        {
            //            if (UseSoundEffects)
            //                _periodCompletedSound.Play();
            //            _periodCompletedAnimationStoryboard.Stop(this);
            //            _stopWatch.Start();
            //            _dispacherTime.Start();
            //            if (CurrentPeriodType == PomodoroPeriodType.Pomodoro || CurrentPeriodType == PomodoroPeriodType.Stopped)
            //                _period++;
            //        }
            //    }
            //    else if ((args[1].ToLowerInvariant() == "/pause"))
            //    {
            //        if (UseSoundEffects)
            //        {
            //            _periodCompletedSound.Stop();
            //            _periodStartSound.Stop();
            //        }
            //        if (_stopWatch.IsRunning)
            //        {
            //            _period--;
            //            _stopWatch.Stop();
            //        }
            //    }
            //    else if ((args[1].ToLowerInvariant() == "/restart"))
            //    {
            //        if (_stopWatch.IsRunning)
            //        {
            //            if (UseSoundEffects)
            //                _periodCompletedSound.Play();
            //            _ticks = 0;
            //            _stopWatch.Restart();
            //        }
            //    }
            //    else if ((args[1].ToLowerInvariant() == "/reset"))
            //    {
            //        ResetTicking();
            //    }
            //    else if ((args[1].ToLowerInvariant() == "/settings"))
            //    {
            //        this.ShowSettings.Execute(this);
            //    }
            //    //else if ((args[1].ToLowerInvariant() == "/homepage"))
            //    //{
            //    //    System.Diagnostics.Process.Start("http://lukaszbanasiak.github.io/YAPA/");
            //    //}
            //}

            return true;
        }

        #endregion


        public ICommand ShowSettings
        {
            get { return _showSettings; }
        }

        public bool UseSoundEffects
        {
            get { return YAPA.Properties.Settings.Default.UseSoundEfects; }
            set
            {
                YAPA.Properties.Settings.Default.UseSoundEfects = value;
                NotifyPropertyChanged("UseSoundEfects");
            }
        }

        public int WorkTime
        {
            get { return YAPA.Properties.Settings.Default.PeriodWork; }
            set
            {
                YAPA.Properties.Settings.Default.PeriodWork = value;
                NotifyPropertyChanged("WorkTime");
            }
        }

        public int BreakTime
        {
            get { return YAPA.Properties.Settings.Default.PeriodShortBreak; }
            set
            {
                YAPA.Properties.Settings.Default.PeriodShortBreak = value;
                NotifyPropertyChanged("BreakTime");
            }
        }

        public int LongBreakTime
        {
            get { return YAPA.Properties.Settings.Default.PeriodLongBreak; }
            set
            {
                YAPA.Properties.Settings.Default.PeriodLongBreak = value;
                NotifyPropertyChanged("LongBreakTime");
            }
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
                return YAPA.Properties.Settings.Default.CountBackwards;
            }
            set
            {
                YAPA.Properties.Settings.Default.CountBackwards = value;
                this.NotifyPropertyChanged("CountBackwards");
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
            DragMove();
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

            this.Close();
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
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
            StopAllSounds();
            DoPomodoroReset();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            DoPauseCurrentPeriod();
        }
    }
}
