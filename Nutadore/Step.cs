﻿using System;
using System.Collections.Generic;
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
	public class Step
	{
		#region props & types
		public List<Sign> voices = new List<Sign>();

		private List<Note> notGuessedNotes = new List<Note>();
		private Score score;
		private Staff trebleStaff;
		private Staff bassStaff;
		private double left;

		private static Brush currentBrush = Brushes.LightSeaGreen;
		private static Brush highlightBrush = Brushes.Gray;

		private bool isCurrent;
		public bool IsCurrent
		{
			get { return isCurrent; }
			set { isCurrent = value; SetColor(); }
		}

		public Perform.HowTo performHowToStaffTreble;
		public Perform.HowTo performHowToStaffBass;

		private bool isHighlighted;
		private Rectangle highlightRect;
		public Rect bounds { get; private set; } = Rect.Empty;
		#endregion

		public Step AddVoice(Sign voice)
		{
			voices.Add(voice);

			return this;
			//if (voice is Chord)
			//{
			//	Chord chord = voice as Chord;
			//	chord.step = this;
			//}
			//else if (voice is Note)
			//{
			//	Note note = voice as Note;
			//	note.step = this;
			//}
		}

		public bool IsBar
		{
			get
			{
				Sign firstVoice = voices.FirstOrDefault();
				return firstVoice != null && firstVoice is Bar;
			}
		}

		public double AddToScore(Score score, Staff trebleStaff, Staff bassStaff, double left)
		{
			// Zapamietujemy, to bo będziemy chcieli dodawać nietrafione nuty.
			this.score = score;
			this.trebleStaff = trebleStaff;
			this.bassStaff = bassStaff;
			this.left = left;

			// Wyliczamy i korygujemy ottave górną i dolną dla całego kroku.
			CalculateAndCorrectPerformHowTo();

			double cursor = left;
			double right = left;

			// Przenieś główki nut drugą strone stem, jesli zachodzą na siebie.
			EliminateHeadsOverlapping();

			// Przesuń znaki chromatyczne, jeśli zachodza na siebie.
			EliminateAccidentalOverlapping();

			// Jeśli nuta w jednym głosie ma przypadkowy znak chromatyczny, nuty w pozostałych 
			// głosach trzeba przesunąć w prawo, by ich główki były w jednej linii.
			double noteHeadOffsetMax = .0;
			for (int phase = 0; phase <= 1; phase++)
			{
				// Dodajemy do score poszczególne głosy.
				foreach (Sign voice in voices)
				{
					INoteOffsets offs = voice as INoteOffsets;
					if (offs != null)
						offs.offset = phase == 0 ? .0 : noteHeadOffsetMax - offs.headOffset;
					double voiceCursor = voice.AddToScore(score, trebleStaff, bassStaff, this, left);
					if (offs != null)
						noteHeadOffsetMax = Math.Max(offs.headOffset, noteHeadOffsetMax);
					if (voiceCursor == -1)
					{
						// Jeden z głosów nie zmieścił się - wycofujemy pozostałe.
						RemoveFromScore(score);
						return -1;
					}
					if (voiceCursor > cursor)
						cursor = voiceCursor;
					if (voice.bounds.Right > right || voice.bounds.Right == -1)
						right = voice.bounds.Right;
				}
				if (phase == 0)
				{
					// Czy jakaś nuta w jakimś głosie miała przypadkowy znak chromatyczny?
					if (noteHeadOffsetMax > 0)
					{
						// Tak - wycofujemy wszystkie głosy i narysujemy je jeszcze raz z przesunięciem.
						foreach (Sign voice in voices)
							voice.RemoveFromScore(score);
						cursor = left;
						right = left;
					}
					else
					{
						// Nie.
						break;
					}
				}
			}

			// Dodajemy do score błędnie wciśnięte nuty.
			foreach (Note notGuessedNote in notGuessedNotes)
			{
				double noteCursor = notGuessedNote.AddToScore(score, trebleStaff, bassStaff, this, left);
				if (noteCursor == -1)
					return -1;
				if (noteCursor > cursor)
					cursor = noteCursor;
				if (notGuessedNote.bounds.Right > right || notGuessedNote.bounds.Right == -1)
					right = notGuessedNote.bounds.Right;
			}

			// Dodajemy prostokąt do reagujący na mysz.
			double top = trebleStaff.StaffPositionToY(StaffPosition.ByLegerAbove(6));
			double bottom = bassStaff.StaffPositionToY(StaffPosition.ByLegerBelow(4));
			highlightRect = new Rectangle
			{
				Width = right - left,
				Height = bottom - top,
				Margin = new Thickness(left, top, 0, 0),
				Fill = Brushes.Transparent,
				Stroke = Brushes.Transparent,
				Tag = score // potrzebne w event handlerze
			};
			highlightRect.MouseEnter += MouseEnter;
			highlightRect.MouseLeave += MouseLeave;
			highlightRect.MouseDown += MouseDown;
			score.Children.Add(highlightRect);
			Canvas.SetZIndex(highlightRect, 100);

			bounds = new Rect(left, top, right - left, bottom - top);

			return cursor;
		}

		private void EliminateHeadsOverlapping()
		{
			// Główki nut zachodzące na siebie przenosi na drugą strone kreseczki.
			List<Note> notes = SelectNotes().OrderBy(note => note).ToList();
			List<Note> notesReversed = new List<Note>();
			bool overlappingFound = false;
			do
			{
				overlappingFound = false;
				foreach (Note note in notes)
				{
					// Wyszukujemy nuty zachodzące na nuty po właściwej i po drugiej stronie kreseczki.
					Func<Note, bool> overlappingCond = n =>
									note.staffType == n.staffType
									&& Math.Abs(note.staffPosition.Number - n.staffPosition.Number) == 0.5
									&& !note.Equals(n);
					bool overlaps = notes.Where(overlappingCond).Count() > 0;
					bool overlapsReversed = notesReversed.Where(overlappingCond).Count() > 0;
					// Nutę przenosimy na drugą stronę, jeśli koliduje z nutami po właściwej 
					// stronie kreseczki i nie koliduje z nutami po drugiej stronie kreseczki.
					if (overlaps && !overlapsReversed)
					{
						notes.Remove(note);
						notesReversed.Add(note);
						overlappingFound = true;
						break;
					}
				}
			} while (overlappingFound);

			// Jeśli nut po drugies stronie jest więcej, to zamieniamy strony, bo to źle wygląda.
			if (notesReversed.Count > notes.Count)
			{
				var notesTmp = notes;
				notes = notesReversed;
				notesReversed = notesTmp;
			}

			// Zazanaczmy nuty po lewej stronie.
			notesReversed.ForEach(note => note.isHeadReversed = true);
		}

		private void EliminateAccidentalOverlapping()
		{
			// Znaki chromatyczne zachodzące na siebie przesuwa w lewą stronę.
			List<List<Note>> cols = new List<List<Note>>() { new List<Note>() };
			cols[0] = SelectNotes()
				.Where(note => note.accidental.type != Accidental.Type.None)
				.OrderBy(note => note)
				.ToList();
			bool overlappingFound = false;
			do
			{
				overlappingFound = false;
				foreach (Note note in cols[0])
					if (overlappingFound = FindAccidentalOverlaping(note, 0, cols))
						break;
			} while (overlappingFound);
		}

		private bool FindAccidentalOverlaping(Note note, int col, List<List<Note>> cols)
		{
			// Wyszukujemy nuty zachodzące na nuty po właściwej i po drugiej stronie kreseczki.
			if (col >= cols.Count())
				return false;
			List<Note> noteOverlapped = cols[col].Where(n =>
					note.staffType == n.staffType
					&& Math.Abs(note.staffPosition.Number - n.staffPosition.Number) <= 1.0
					&& !note.Equals(n))
				.ToList();
			if (noteOverlapped.Count() == 0)
				return false;
			if (col + 1 >= cols.Count())
				cols.Add(new List<Note>());
			cols[col + 1].Add(note);
			cols[col].Remove(note);
			FindAccidentalOverlaping(note, col + 1, cols);
			return true;
		}

		public void RemoveFromScore(Score score)
		{
			foreach (Sign sign in voices)
				sign.RemoveFromScore(score);

			foreach (Note notGuessedNote in notGuessedNotes)
				notGuessedNote.RemoveFromScore(score);

			score.Children.Remove(highlightRect);
			highlightRect = null;
		}

		private void SetColor()
		{
			if (isCurrent && isHighlighted)
			{
				highlightRect.Fill = currentBrush;
				highlightRect.Stroke = currentBrush;
				highlightRect.Opacity = 0.3;
			}
			else if (isCurrent && !isHighlighted)
			{
				highlightRect.Fill = currentBrush;
				highlightRect.Stroke = currentBrush;
				highlightRect.Opacity = 0.2;

			}
			else if (!isCurrent && isHighlighted)
			{
				highlightRect.Fill = highlightBrush;
				highlightRect.Stroke = highlightBrush;
				highlightRect.Opacity = 0.1;
			}
			else if (!isCurrent && !isHighlighted)
			{
				highlightRect.Fill = Brushes.Transparent;
				highlightRect.Stroke = Brushes.Transparent;
			}
		}

		private void CalculateAndCorrectPerformHowTo()
		{
			// Wyszukujemy wszystkie nuty w kroku.
			List<Note> stepNotes = SelectNotes();

			// Dodajemy również nuty błędnie wciśniętych klawiszy.
			stepNotes.AddRange(notGuessedNotes);

			// Wyszukaj wszystkie nuty akordu leżące na pięcilinii wilonowej.
			List<Note> trebleNotes = stepNotes.FindAll(note => note.staffType == Staff.Type.Treble);

			// Czy sa jakieś nuty wymagające zmiany wysokości wykonania na pieciolinii wiolinowej?
			if (trebleNotes.Any(note => note.performHowTo == Perform.HowTo.TwoOctaveHigher))
			{
				// Sa nuty wymagające zmiany wysokości wykonania o dwie oktawy wyżej.
				// Wyszukaj wszystkie nuty wymagające wykonania o jedną oktawę wyżej
				// i zmień je na wymagające wykonania o dwie oktawy wyżej.
				trebleNotes
					.FindAll(note => note.performHowTo == Perform.HowTo.OneOctaveHigher)
					.ForEach(note =>
					{
						note.performHowTo = Perform.HowTo.TwoOctaveHigher;
						note.staffPosition.Number -= 3.5;
					});

				// Wyszukaj wszystkie nuty nie wymagające zmiany wykonania i zmień je na
				// wymagające wykonania o dwie oktawy wyżej.
				trebleNotes
					.FindAll(note => note.performHowTo == Perform.HowTo.AtPlace)
					.ForEach(note =>
					{
						note.performHowTo = Perform.HowTo.TwoOctaveHigher;
						note.staffPosition.Number -= 3.5 * 2;
					});

				// Teraz wszystkie nuty akordu leżące na pięciolinii wiolinowej
				// będą wykonywane o dwie oktawy wyzej.
				performHowToStaffTreble = Perform.HowTo.TwoOctaveHigher;
			}
			else if (trebleNotes.Any(note => note.performHowTo == Perform.HowTo.OneOctaveHigher))
			{
				// Są nuty wymagające zmiany wysokści wykonania o oktawę wyżej.
				// Wyszukaj wszystkie nuty nie wymagające zmiany wykonania i zmień je na
				// wymagające wykonania o jedną oktawę wyżej.
				trebleNotes
					.FindAll(note => note.performHowTo == Perform.HowTo.AtPlace)
					.ForEach(note =>
					{
						note.performHowTo = Perform.HowTo.OneOctaveHigher;
						note.staffPosition.Number -= 3.5; // 0.0; // To ma tak byc!
					});

				// Teraz wszyskie nuty akordu leżące na pęciolinii wiolinowej będą
				// wykonywane o oktawę wyżej.
				performHowToStaffTreble = Perform.HowTo.OneOctaveHigher;
			}

			// Wyszukaj wszystkie nuty akordu leżące na pięcilinii basowej.
			List<Note> bassNotes = stepNotes.FindAll(note => note.staffType == Staff.Type.Bass);

			// Czy sa jakieś nuty wymagające zmiany wysokości wykonania?
			if (bassNotes.Any(note => note.performHowTo == Perform.HowTo.OneOctaveLower))
			{
				// Jest przynajmniej jedna nuta wymagająca wykonania o oktawę niżej.
				// Wyszukaj wszystkie nuty nie wymagające zmiany wykonania i zmień je na
				// wymagające wykonania o jedną oktawę niżej.
				bassNotes
					.FindAll(note => note.performHowTo == Perform.HowTo.AtPlace)
					.ForEach(note =>
					{
						note.performHowTo = Perform.HowTo.OneOctaveLower;
						note.staffPosition.Number += 3.5;
					});

				// Teraz wszystkie nuty leżące na pięciolinii basowej
				// będą wykonywane o oktawę nizej.
				performHowToStaffBass = Perform.HowTo.OneOctaveLower;
			}
		}

		public List<Note> SelectNotes()
		{
			return voices
				.Where(voice => voice is Chord)
				.SelectMany(voice => (voice as Chord).notes)
				.Union(
					voices
						.Where(voice => voice is Note)
						.Select(voice => voice as Note)
				).ToList();
			/*
			List<Note> notes = new List<Note>();
			foreach (Sign voice in voices)
			{
				if (voice is Chord)
				{
					Chord chord = voice as Chord;
					notes.AddRange(chord.notes);
				}
				else if (voice is Note)
				{
					Note note = voice as Note;
					notes.Add(note);
				}
			}

			return notes;
			*/
		}

		public void Highlight(bool highlight)
		{
			isHighlighted = highlight;
			SetColor();
		}

		public void MouseEnter(object sender, MouseEventArgs e)
		{
			isHighlighted = true;
			SetColor();

			Score score = (sender as Rectangle).Tag as Score;
			score.FireEvent(SelectNotes(), ScoreEventArgs.EventType.HighlightedOn);
		}

		public void MouseLeave(object sender, MouseEventArgs e)
		{
			isHighlighted = false;
			SetColor();

			Score score = (sender as Rectangle).Tag as Score;
			score.FireEvent(SelectNotes(), ScoreEventArgs.EventType.HighlightedOff);
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			Score score = (sender as Rectangle).Tag as Score;
			score.CurrentStep = this;

			score.FireEvent(SelectNotes(), ScoreEventArgs.EventType.Selected);
		}

		public void KeyDown(Note noteDown)
		{
			// Czy trafiono wciśnięto właściwy klawisz?
			Note note = SelectNotes().Find(n => n.Equals(noteDown));
			if (note != null)
			{
				// Tak, zaznaczmy nutę na zielono.
				note.Guessed = true;
			}
			else
			{
				// Nie, dodajemy czerwoną nutę.
				note = new Note(noteDown.letter, noteDown.accidental.type, noteDown.octave);
				note.AddToScore(score, trebleStaff, bassStaff, this, left);
				note.Guessed = false;
				notGuessedNotes.Add(note);
				// TODO: dodana czerwona nuta może wymagać zmiany Perform.HowTo
			}
		}

		public void KeyUp(Note noteDown)
		{
			// Czy trafiono we właściwy klawisz?
			Note note = SelectNotes().Find(n => n.Equals(noteDown));
			if (note != null)
			{
				// Tak, zmieniamy zielony kolor na czarny.
				note.Guessed = null;
			}
			else
			{
				// Nie, usuwamy czerwoną nutę.
				note = notGuessedNotes.Find(n => n.Equals(noteDown));
				if (note == null)
					return;
				note.RemoveFromScore(score);
				notGuessedNotes.Remove(note);
			}
		}

	}
}
