﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Nutadore
{
	public class Key
	{
		private static readonly SolidColorBrush whiteKeyUpBrush = Brushes.Snow;
		private static readonly SolidColorBrush blackKeyUpBrush = Brushes.Black;

		// Możliwe stany:
		// Up (biały/czarny)
		// Up Guess (żółty jasny/ciemnu)
		// Down (mocno szary jasny/ciemny)
		// Down Hit (zielony jasny/ciemny)
		// Down !Hit (czerwony jasny/ciemny)
		// Dodatkowo podświetlenie rozjaśniające kolory

		private bool _highlighted;
		public bool Highlighted
		{
			get { return _highlighted; }
			set { _highlighted = value; SetColor(); }
		}

		private bool _guess;
		public bool Guess
		{
			get { return _guess; }
			set { _guess = value;  SetColor(); }
		}

		private bool? _hit;
		public bool? Hit
		{
			get { return _hit; }
			set {
				_hit = value;
				if (_hit != null)
					_down = true;
				SetColor(); }
		}

		private bool _down;
		public bool Down
		{
			get { return _down; }
			set { _down = value;  SetColor(); }
		}

		public enum State
		{
			Up,
			UpGuess,
			Down,
			DownGuess,
			DownGuessHit,
			DownGuessMissed
		}

		//public enum State
		//{
		//	Up,
		//	Down,
		//	Hit,
		//	Missed
		//}

		public State state = State.Up;
		//private bool isHighlighted;
		private Rectangle highlightRectangle;

		private readonly int keyNoInOctave;
		public readonly Note note;
		public readonly bool isWhite;

		public Key(int keyNo)
		{
			// Wyznaczam oktawe i numer klawisza w oktawie dla klawisza.
			const int keysInOctave = 12;
			Note.Octave octave;
			if (keyNo >= 0 && keyNo <= 2)
			{
				// Subcontra A, A#, H
				octave = Note.Octave.SubContra;
				keyNoInOctave = 9 + keyNo;
			}
			else if (keyNo >= 3 && keyNo <= 86)
			{
				// Great..FourLined C..H
				octave = (Note.Octave)((keyNo - 3) / keysInOctave + 1);
				keyNoInOctave = (keyNo - 3) % keysInOctave;
			}
			else if (keyNo == 87)
			{
				// FiveLined C
				octave = Note.Octave.FiveLined;
				keyNoInOctave = 0;
			}
			else
			{
				string message = string.Format("Numer klawisza {0} spoza zakresu [0, 88]!", keyNo);
				throw new ArgumentOutOfRangeException("keyNo", message);
			}

			// Wyznaczam literę nuty dla klawisza.
			Note.Letter letter;
			Note.Accidental accidental = Note.Accidental.None;
			switch (keyNoInOctave)
			{
				case 0:
					letter = Note.Letter.C;
					break;
				case 1:
					accidental = Note.Accidental.Sharp;
					letter = Note.Letter.C;
					break;
				case 2:
					letter = Note.Letter.D;
					break;
				case 3:
					accidental = Note.Accidental.Sharp;
					letter = Note.Letter.D;
					break;
				case 4:
					letter = Note.Letter.E;
					break;
				case 5:
					letter = Note.Letter.F;
					break;
				case 6:
					accidental = Note.Accidental.Sharp;
					letter = Note.Letter.F;
					break;
				case 7:
					letter = Note.Letter.G;
					break;
				case 8:
					accidental = Note.Accidental.Sharp;
					letter = Note.Letter.G;
					break;
				case 9:
					letter = Note.Letter.A;
					break;
				case 10:
					accidental = Note.Accidental.Sharp;
					letter = Note.Letter.A;
					break;
				case 11:
					letter = Note.Letter.H;
					break;
				default:
					throw new ArgumentOutOfRangeException("keyNoInOctave");
			}

			// Wyznaczam nute skojarzona z klawiszem.
			note = new Note(letter, accidental, octave);

			// Wyznaczam kolor klawisza.
			isWhite =
				keyNoInOctave == 0 ||
				keyNoInOctave == 2 ||
				keyNoInOctave == 4 ||
				keyNoInOctave == 5 ||
				keyNoInOctave == 7 ||
				keyNoInOctave == 9 ||
				keyNoInOctave == 11;
		}

		public double AddToCanvas(/*Score score, */Keyboard keyboard)
		{
			// szerokość klawiszy białych i czarnych
			double whiteWidth = keyboard.ActualWidth / Keyboard.numberOfWhiteKeys;
			double blackWidth = whiteWidth * 0.6;

			// wysokość klawiszy białych i czarnych
			double whiteHeight = whiteWidth * 4.0;
			double blackHeight = whiteHeight * 0.6;

			// serokość i długość klawisza
			double width = isWhite ? whiteWidth : blackWidth;
			double height = isWhite ? whiteHeight : blackHeight;

			// położenie klawisza wynikające z położenia w oktawie
			double left = 0;
			switch(keyNoInOctave)
			{
				case 0: left = 0; break;
				case 1: left = whiteWidth - blackWidth / 2; break;
				case 2: left = whiteWidth; break;
				case 3: left = whiteWidth * 2 - blackWidth / 2; break;
				case 4: left = whiteWidth * 2; break;
				case 5: left = whiteWidth * 3; break;
				case 6: left = whiteWidth * 4 - blackWidth / 2; break;
				case 7: left = whiteWidth * 4; break;
				case 8: left = whiteWidth * 5 - blackWidth / 2; break;
				case 9: left = whiteWidth * 5; break;
				case 10: left = whiteWidth * 6 - blackWidth / 2; break;
				case 11: left = whiteWidth * 6; break;
			}
			// przesunięcie wynikające z oktawy
			left += (int)note.octave * whiteWidth * 7;
			// przesunięcie wynikające z tego że w oktawie SubContra są tylko klawisze A, A# i H
			left -= whiteWidth * 5;

			// Rysuje klawisz.
			Rectangle keyRectangle = new Rectangle
			{
				Width = width,
				Height = height,
				Margin = new Thickness(left, 0, 0, 0),
				Fill = isWhite ? whiteKeyUpBrush : blackKeyUpBrush,
				Stroke = Brushes.Black,
				StrokeThickness = isWhite ? 0.2 : 0.8
			};
			Canvas.SetZIndex(keyRectangle, isWhite ? 0 : 2);
			keyboard.Children.Add(keyRectangle);

			// Transparentny prostokąt modyfikujący kolor klawisza i reagujący na myszę.
			highlightRectangle = new Rectangle
			{
				Width = width,
				Height = height,
				Margin = new Thickness(left, 0, 0, 0),
				Fill = Brushes.Transparent,
				Stroke = Brushes.Transparent,
				Tag = keyboard
			};
			Canvas.SetZIndex(highlightRectangle, isWhite ? 1 : 3);
			keyboard.Children.Add(highlightRectangle);
			highlightRectangle.MouseEnter += MouseEnter;
			highlightRectangle.MouseLeave += MouseLeave;
			highlightRectangle.MouseDown += MouseDown;
			highlightRectangle.MouseUp += MouseUp;

			// Rysuję nazwy oktaw.
			double keyboardHeight = whiteHeight;
			if (note.letter == Note.Letter.C && isWhite)
			{
				const string familyName = "Consolas";
				double fontSize = 12;
				TextBlock octaveNameTextBlock = new TextBlock
				{
					FontFamily = new FontFamily(familyName),
					FontSize = fontSize,
					Text = note.octave.ToString(),
					Foreground = Brushes.Red,
					//Margin = new Thickness(line.X1, line.Y2, 0, 0)
					Margin = new Thickness(left + 3, height, 0, 0)
				};
				// Wyznaczamy wysokość napisu.
				FormattedText ft = new FormattedText(
					octaveNameTextBlock.Text,
					CultureInfo.GetCultureInfo("en-us"),
					FlowDirection.LeftToRight,
					new Typeface(familyName),
					fontSize,
					Brushes.Black);
				keyboardHeight += ft.Height;
				keyboard.Children.Add(octaveNameTextBlock);
			}

			// Rysuję linie oddzielające klawisze i linie oddzielające oktawy.
			if (isWhite)
			{
				Line line = new Line
				{
					X1 = left,
					Y1 = 0,
					X2 = left,
					//Y2 = height,
					Y2 = keyboardHeight,
					Stroke = note.letter == Note.Letter.C && isWhite ? Brushes.Red : Brushes.Black,
					StrokeThickness = note.letter == Note.Letter.C && isWhite ? 1.0 : 0.4
				};
				Canvas.SetZIndex(line, 0);
				keyboard.Children.Add(line);
			}

			SetColor();

			return keyboardHeight;
		}

		private void MouseEnter(object sender, MouseEventArgs e)
		{
			MarkAsHighlighted(true);
		}

		private void MouseLeave(object sender, MouseEventArgs e)
		{
			MarkAsHighlighted(false);
		}

		private void MouseDown(object sender, MouseButtonEventArgs e)
		{
			//if (state == State.Up)
			//	state = State.Down;
			//else if (state == State.Down)
			//	state = State.Up;
			//SetColor();
			Keyboard keyboard = (sender as Rectangle).Tag as Keyboard;
			//keyboard.Check(this.note);
		}

		private void MouseUp(object sender, MouseButtonEventArgs e)
		{
			//if (state == State.Hit)
			//	state = State.Down;
			//else if (state == State.Missed)
			//	state = State.Up;
			//SetColor();
		}

		public void MarkAs(State state)
		{
			//this.state = state;
			//SetColor();
		}

		public void MarkAsHighlighted(bool isHighlighted)
		{
			//this.isHighlighted = isHighlighted;
			//SetColor();
		}

		private void SetColor()
		{
			SolidColorBrush brush = Brushes.Black.Clone();
			brush.Opacity = 1;


			if (Guess)
			{
				brush.Color = isWhite ? Colors.Yellow: Colors.Gold;
			}

			if (Down)
			{
				if (Hit == true)
				{
					brush.Color = isWhite ? Colors.LightGreen : Colors.Green;
				}
				else if (Hit == false)
				{
					brush.Color = isWhite ? Colors.Red : Colors.DarkRed;
				}
				else if (Hit == null)
				{
					brush.Color = Color.Add(brush.Color, isWhite ? Colors.DarkGray : Colors.Gray);
				}
			}

			if (Highlighted)
			{
				if (brush.Color == Colors.Black)
				{
					brush.Color = isWhite ? Colors.LightGray : Colors.Gray;
				}
				else
				{
					brush.Color = Color.Add(brush.Color, isWhite ? Colors.DarkGray : Colors.DarkGray);
				}
			}

			if (brush.Color == Colors.Black)
			{
				brush.Opacity = 0;
			}

			highlightRectangle.Fill = brush;
		}
	}
}
