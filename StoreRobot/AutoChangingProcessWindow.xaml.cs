using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using StoreRobot.Models;
using StoreRobot.Utils;

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for AutoChangingProccessWindow.xaml
    /// </summary>
    public partial class AutoChangingProcessWindow : Window
    {
        private bool _IsSafirSelected;
        private bool _IsPakhshSelected;
        private bool _IsDigiSelected;
        private TimeSpan _delay;
        private TimeSpan _duration;
        private DateTime _finished;
        private bool _isInfinite;
        DispatcherTimer _timer;
        TimeSpan _time;
        DispatcherTimer _delayTimer;
        TimeSpan _delayTime;
        private ApplicationDbContext _db = new();
        CancellationTokenSource _source = new();
        public AutoChangingProcessWindow(bool isSafirSelected, bool isPakhshSelected, bool isDigiSelected,
            TimeSpan delay, TimeSpan duration, bool isInfinite)
        {
            _IsSafirSelected = isSafirSelected;
            _IsPakhshSelected = isPakhshSelected;
            _IsDigiSelected = isDigiSelected;
            _delay = delay;
            _duration = duration;
            _isInfinite = isInfinite;
            _finished = _duration != TimeSpan.MaxValue
                ? DateTime.Now.AddMinutes(_duration.TotalMinutes)
                : DateTime.MaxValue;
            InitializeComponent();
            if (!_isInfinite)
            {
                DurationTextBlock.Visibility = Visibility.Visible;
                _time = duration;

                _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                {
                    DurationTextBlock.Text = _time.ToString("c").ToFarsi();
                    if (_time == TimeSpan.Zero) _timer.Stop();
                    _time = _time.Add(TimeSpan.FromSeconds(-1));
                }, Application.Current.Dispatcher);

                _timer.Start();
            }
            else
            {
                Height = double.Parse("380");
                DurationTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        #region TopRow
        private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CancellationToken token = _source.Token;
            try
            {
                await Task.Run(() =>
                {
                    while (DateTime.Compare(_finished, DateTime.Now) > 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        Dispatcher.Invoke(() =>
                        {
                            ChangingProcessWindow changingProcessWindow =
                                new(_IsSafirSelected, _IsPakhshSelected, _IsDigiSelected, ProcessMode.Auto);
                            changingProcessWindow.ShowDialog();

                            _delayTime = _delay;

                            _delayTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                            {
                                DelayTextBlock.Text = _delayTime.ToString("c").ToFarsi();
                                if (_delayTime == TimeSpan.Zero) _delayTimer.Stop();
                                _delayTime = _delayTime.Add(TimeSpan.FromSeconds(-1));
                            }, Application.Current.Dispatcher);

                            _delayTimer.Start();
                        });
                        Thread.Sleep(_delay);
                    }

                    //finish process
                    Dispatcher.Invoke(() =>
                    {
                        FinishedWindow finishedWindow = new();
                        finishedWindow.ShowDialog();
                        Close();
                    });
                }, token);
            }
            catch (OperationCanceledException)
            {
                //ignored
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _source.Cancel();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            WarningMessage warningMessage = new();
            warningMessage.ShowDialog();
            if (warningMessage.DialogResult.Value)
                Close();
        }
    }
}
