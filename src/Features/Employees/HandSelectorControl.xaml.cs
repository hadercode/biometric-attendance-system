using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LectorHuellas.Core.Models;

namespace LectorHuellas.Features.Employees
{
    public partial class HandSelectorControl : UserControl
    {
        public static readonly DependencyProperty SelectedFingerProperty =
            DependencyProperty.Register("SelectedFinger", typeof(FingerType?), typeof(HandSelectorControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFingerChanged));

        public static readonly DependencyProperty EnrolledFingersProperty =
            DependencyProperty.Register("EnrolledFingers", typeof(IEnumerable<FingerType>), typeof(HandSelectorControl),
                new PropertyMetadata(null, OnEnrolledFingersChanged));

        public FingerType? SelectedFinger
        {
            get => (FingerType?)GetValue(SelectedFingerProperty);
            set => SetValue(SelectedFingerProperty, value);
        }

        public IEnumerable<FingerType>? EnrolledFingers
        {
            get => (IEnumerable<FingerType>?)GetValue(EnrolledFingersProperty);
            set => SetValue(EnrolledFingersProperty, value);
        }

        private readonly Dictionary<int, Button> _fingerButtons = new();

        public HandSelectorControl()
        {
            InitializeComponent();
            Loaded += (_, _) => CollectButtons();
        }

        private void CollectButtons()
        {
            _fingerButtons[0] = BtnLeftPinky;
            _fingerButtons[1] = BtnLeftRing;
            _fingerButtons[2] = BtnLeftMiddle;
            _fingerButtons[3] = BtnLeftIndex;
            _fingerButtons[4] = BtnLeftThumb;
            _fingerButtons[5] = BtnRightThumb;
            _fingerButtons[6] = BtnRightIndex;
            _fingerButtons[7] = BtnRightMiddle;
            _fingerButtons[8] = BtnRightRing;
            _fingerButtons[9] = BtnRightPinky;
            RefreshVisuals();
        }

        private void FingerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int idx))
            {
                SelectedFinger = (FingerType)idx;
            }
        }

        private static void OnSelectedFingerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HandSelectorControl ctrl)
                ctrl.RefreshVisuals();
        }

        private static void OnEnrolledFingersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HandSelectorControl ctrl)
                ctrl.RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (_fingerButtons.Count == 0) return;

            var enrolled = new HashSet<FingerType>();
            if (EnrolledFingers != null)
                foreach (var f in EnrolledFingers)
                    enrolled.Add(f);

            int enrolledCount = enrolled.Count;

            foreach (var kvp in _fingerButtons)
            {
                var fingerType = (FingerType)kvp.Key;
                var btn = kvp.Value;
                bool isSelected = SelectedFinger.HasValue && SelectedFinger.Value == fingerType;
                bool isEnrolled = enrolled.Contains(fingerType);

                if (isSelected)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F3A5F"));
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58A6FF"));
                    btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58A6FF"));
                }
                else if (isEnrolled)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A3F2E"));
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950"));
                    btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950"));
                }
                else
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D333B"));
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444C56"));
                    btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E"));
                }
            }

            SelectedFingerText.Text = SelectedFinger.HasValue
                ? $"Dedo seleccionado: {SelectedFinger.Value.ToDisplayName()}"
                : "Haga clic en un dedo para seleccionarlo";

            EnrolledCountText.Text = $"{enrolledCount} de 10 huellas registradas (mínimo 1)";
        }
    }
}
