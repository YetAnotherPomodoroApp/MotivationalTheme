using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window, ISettingsViewModel, INotifyPropertyChanged
    {
        private IMainViewModel _host;
        private ICommand _saveSettings;
        private ItemRepository _itemRepository;

        // INPC support
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Window constructor.
        /// </summary>
        public Settings(IMainViewModel host)
        {
            InitializeComponent();
            this.DataContext = this;
            _host = host;
            _saveSettings = new SaveSettings(this);

            MouseLeftButtonDown += Settings_MouseLeftButtonDown;
            _itemRepository = new ItemRepository();

            Loaded += Settings_Loaded;
        }

        private async void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
                Calendar cal = dfi.Calendar;

                var pomodoros =
                    _itemRepository.GetPomodoros()
                        .Select(
                            x => new { week = cal.GetWeekOfYear(x.DateTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek), x });
                int max = pomodoros.Max(x => x.x.Count);

                foreach (var pomodoro in pomodoros.GroupBy(x => x.week))
                {
                    var week = pomodoro.Select(x => x.x.ToPomodoroViewModel(GetLevelFromCount(x.x.Count, max)));
                    Dispatcher.Invoke(() =>
                    {
                        WeekStackPanel.Children.Add(new PomodoroWeek(week));
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    DayPanel.Visibility = Visibility.Visible;
                    LoadingPanel.Visibility = Visibility.Collapsed;
                });
            });
        }

        private  PomodoroLevelEnum GetLevelFromCount(int count, int maxCount)
        {
            if (count == 0)
            {
                return PomodoroLevelEnum.Level0;
            }
            if (maxCount <= 4)
            {
                return PomodoroLevelEnum.Level4;
            }
            var level = (double)count / maxCount;
            if (level < 0.25)
            {
                return PomodoroLevelEnum.Level1;
            }
            else if (level < 0.50)
            {
                return PomodoroLevelEnum.Level2;
            }
            else if (level < 0.75)
            {
                return PomodoroLevelEnum.Level3;
            }

            return PomodoroLevelEnum.Level4;
        }

        /// <summary>
        /// Used to support dragging the window around.
        /// </summary>
        private void Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        /// <summary>
        /// Closes this view.
        /// </summary>
        public void CloseSettings()
        {
            this.Close();
        }

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        private double _timerForegroundOpacity;
        public double TimerForegroundOpacity
        {
            get { return _timerForegroundOpacity; }
            set
            {
                _timerForegroundOpacity = value;
                _host.TimerForegroundOpacity = value;
                NotifyPropertyChanged("TimerForegroundOpacity");
            }
        }

        /// <summary>
        /// The desired opacity of the 
        /// </summary>
        private double _timerShadowOpacity;
        public double TimerShadowOpacity
        {
            get { return _timerShadowOpacity; }
            set
            {
                _timerShadowOpacity = value;
                _host.TimerShadowOpacity = value;
                NotifyPropertyChanged("TimerShadowOpacity");
            }
        }

        /// <summary>
        /// True if we are to use white text to render;
        /// otherwise, false.
        /// </summary>
        private bool _useLightTheme;
        public bool UseLightTheme
        {
            get { return _useLightTheme; }
            set
            {
                _useLightTheme = value;

                // Set colors
                _host.TimerForegroundColor = (_useLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_TIMER_FOREGROUND) : Utils.HexToBrush(Const.COLOR_DARK_TIMER_FOREGROUND));
                _host.TimerShadowColor = (_useLightTheme ? Utils.HexToColor(Const.COLOR_LIGHT_TIMER_SHADOW) : Utils.HexToColor(Const.COLOR_DARK_TIMER_SHADOW));
                _host.WindowBackgroundColor = (_useLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_BACKGROUND) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_BACKGROUND));
                _host.WindowBackground2Color = (_useLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_BACKGROUND2) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_BACKGROUND2));
                _host.WindowForegroundColor = (_useLightTheme ? Utils.HexToBrush(Const.COLOR_LIGHT_WINDOW_FOREGROUND) : Utils.HexToBrush(Const.COLOR_DARK_WINDOW_FOREGROUND));
                _host.WindowShadowColor = (_useLightTheme ? Utils.HexToColor(Const.COLOR_LIGHT_WINDOW_SHADOW) : Utils.HexToColor(Const.COLOR_DARK_WINDOW_SHADOW));
                _host.WindowShadowOpacity = (_useLightTheme ? Const.COLOR_LIGHT_WINDOW_SHADOW_OPACITY : Const.COLOR_DARK_WINDOW_SHADOW_OPACITY);

                NotifyPropertyChanged("UseLightTheme");
            }
        }

        private string _accentColor;
        public string AccentColor
        {
            get { return _accentColor; }
            set
            {
                _accentColor = value;
                _host.AccentColor = Utils.HexToBrush(value);
                NotifyPropertyChanged("AccentColor");
            }
        }

        private bool _useSoundEfects;
        public bool UseSoundEfects
        {
            get { return _useSoundEfects; }
            set
            {
                _useSoundEfects = value;
                _host.UseSoundEffects = value;
                NotifyPropertyChanged("UseSoundEfects");
            }
        }

        private int _workTime;
        public int WorkTime
        {
            get { return _workTime; }
            set
            {
                _workTime = value;
                _host.WorkTime = value;
                NotifyPropertyChanged("WorkTime");
            }
        }

        private int _breakTime;
        public int BreakTime
        {
            get { return _breakTime; }
            set
            {
                _breakTime = value;
                _host.BreakTime = value;
                NotifyPropertyChanged("BreakTime");
            }
        }

        private int _longBreakTime;
        public int LongBreakTime
        {
            get { return _longBreakTime; }
            set
            {
                _longBreakTime = value;
                _host.LongBreakTime = value;

                NotifyPropertyChanged("LongBreakTime");
            }
        }

        private bool _countBackwards;
        public bool CountBackwards
        {
            get
            {
                return _countBackwards;
            }
            set
            {
                _countBackwards = value;
                _host.CountBackwards = value;
                this.NotifyPropertyChanged("CountBackwards");
            }
        }

        /// <summary>
        /// Command invoked when user clicks 'Done'
        /// </summary>
        public ICommand SaveSettings
        {
            get { return _saveSettings; }
        }

        /// <summary>
        /// Used to raise change notifications to other consumers.
        /// </summary>
        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        /// <summary>
        /// Handles a user click on the navigation link.
        /// </summary>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }
    }
}
