﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nutadore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Score score;

        public MainWindow()
        {
            InitializeComponent();

            score = new Score(canvas, Scale.Based.C, Scale.Type.Major);

            for (int i = 0; i < 10; i++)
                score.Add(new Note(score));

            //score.Paint();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            score.Show();
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //base.OnPreviewMouseWheel(e);
            //if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
            //    Keyboard.IsKeyDown(Key.RightCtrl))
            {
                const double magnificationDelta = 0.2;
                if (e.Delta < 0)
                {
                    if (score.Magnification > 0.5)
                        score.Magnification -= magnificationDelta;
                }
                else
                {
                    if (score.Magnification < 5)
                        score.Magnification += magnificationDelta;
                }
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //base.OnPreviewMouseDown(e);
            //if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
            //    Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.ChangedButton == MouseButton.Middle)
                    score.Magnification = 1.0;
            }
        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            //canvas.Children.Remove(Sign.ttt);
        }
    }
}
