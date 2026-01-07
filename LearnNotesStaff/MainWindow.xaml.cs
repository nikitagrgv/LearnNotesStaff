using System.Windows;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;

namespace LearnNotesStaff
{
	public partial class MainWindow : Window
	{
		private InputDevice _inputDevice;
		private int _targetMidiNote;
		private BlackKeyType _targetBlackKeyType = BlackKeyType.Sharp;
		private Random _random = new Random();

		private const double LineSpacing = 20; // Distance between staff lines
		private const double StaffTop = 100;   // Where the first line starts

		public MainWindow()
		{
			InitializeComponent();
			SetupMidi();
			GenerateNewNote();
		}

		private void SetupMidi()
		{
			try
			{
				InputDevice device = InputDevice.GetByIndex(0);
				_inputDevice = device;
				_inputDevice.EventReceived += OnMidiEventReceived;
				_inputDevice.StartEventsListening();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Could not find MIDI device: {ex.Message}");
			}
		}

		private void StaffCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			RedrawCanvas();
		}

		private void RedrawCanvas()
		{
			ClearCanvas();

			int e4 = NoteUtilities.GetNoteNumber(NoteName.E, 4);

			DrawStaff(StaffTop);
			DrawOtherStaff(_targetMidiNote, e4, StaffTop, _targetBlackKeyType);
			DrawNote(_targetMidiNote, e4, StaffTop, _targetBlackKeyType);
		}

		private void DrawOtherStaff(
			int targetMidiNote,
			int bottomNote,
			double staffTop,
			BlackKeyType targetBlackKeyType)
		{
			int width = 15;
			int notePos = GetNoteShelfPosition(targetMidiNote, bottomNote, targetBlackKeyType);
			int lines = (-notePos) / 2 + 5;
			int start = Math.Min(0, lines);
			int end = Math.Max(0, lines);

			for (int i = start; i < end; i++)
			{
				if (i >= 0 && i < 5)
					continue;

				Line line = new Line
				{
					X1 = -width,
					X2 = width,
					Y1 = staffTop + (i * LineSpacing),
					Y2 = staffTop + (i * LineSpacing),
					Stroke = Brushes.Black,
					StrokeThickness = 2
				};
				StaffCanvas.Children.Add(line);
			}

			if (notePos > 9)
			{
				Line line = new Line
				{
					X1 = -width,
					X2 = width,
					Y1 = staffTop + ((start - 1) * LineSpacing),
					Y2 = staffTop + ((start - 1) * LineSpacing),
					Stroke = Brushes.Black,
					StrokeThickness = 2
				};
				StaffCanvas.Children.Add(line);
			}
		}

		private void ClearCanvas()
		{
			StaffCanvas.Children.Clear();
		}

		private void DrawStaff(double staffTop)
		{
			for (int i = 0; i < 5; i++)
			{
				Line line = new Line
				{
					X1 = -100,
					X2 = 100,
					Y1 = staffTop + (i * LineSpacing),
					Y2 = staffTop + (i * LineSpacing),
					Stroke = Brushes.Black,
					StrokeThickness = 2
				};
				StaffCanvas.Children.Add(line);
			}
		}

		enum BlackKeyType
		{
			Sharp,
			Flat,
		}

		private static int WhiteKeysDifference(int firstNote, int secondNote)
		{
			int bigger = int.Max(firstNote, secondNote);
			int smaller = int.Min(firstNote, secondNote);

			int difference = 0;
			while (smaller < bigger)
			{
				difference += IsWhiteKey(smaller) ? 1 : 0;
				smaller++;
			}

			return firstNote > secondNote ? difference : -difference;
		}

		private static int GetNoteShelfPosition(int midiNumber, int bottomNote, BlackKeyType blackKeyType)
		{
			int whiteKey = ToWhiteNote(midiNumber, blackKeyType);
			int position = WhiteKeysDifference(whiteKey, bottomNote);
			return position;
		}

		private void DrawNote(int midiNumber, int bottomNote, double staffTop, BlackKeyType blackKeyType)
		{
			int position = GetNoteShelfPosition(midiNumber, bottomNote, blackKeyType);
			double y = staffTop + (4 * LineSpacing) - (position * (LineSpacing / 2));

			// 2. Create the circle (the note head)
			Ellipse noteHead = new Ellipse
			{
				Width = 20,
				Height = 15, // Notes are slightly oval
				Fill = Brushes.Black
			};

			// 3. Position it on the Canvas
			Canvas.SetLeft(noteHead, -noteHead.Width / 2);
			Canvas.SetTop(noteHead, y - (noteHead.Height / 2));

			StaffCanvas.Children.Add(noteHead);
		}

		private static bool IsWhiteKey(int midiNumber)
		{
			int noteIndex = midiNumber % 12;
			// Indices for C, D, E, F, G, A, B
			int[] whiteKeyIndices =
			{
				0, 2, 4, 5, 7, 9, 11
			};
			return whiteKeyIndices.Contains(noteIndex);
		}

		private static int SharpToNote(int midiNumber)
		{
			while (!IsWhiteKey(midiNumber))
			{
				midiNumber--;
			}

			return midiNumber;
		}

		private static int FlatToNote(int midiNumber)
		{
			while (!IsWhiteKey(midiNumber))
			{
				midiNumber++;
			}

			return midiNumber;
		}

		private static int ToWhiteNote(int midiNumber, BlackKeyType blackKeyType)
		{
			int whiteNote = blackKeyType == BlackKeyType.Sharp ? SharpToNote(midiNumber) : FlatToNote(midiNumber);
			Debug.Assert(int.Abs(midiNumber - whiteNote) <= 1);
			Debug.Assert(blackKeyType != BlackKeyType.Sharp ||
			             GetNoteName(midiNumber, BlackKeyType.Sharp).Replace("#", "") ==
			             GetNoteName(whiteNote, BlackKeyType.Sharp));
			return whiteNote;
		}

		private static string GetNoteName(int midiNumber, BlackKeyType blackKeyType)
		{
			SevenBitNumber note = new((byte)midiNumber);
			return $"{NoteUtilities.GetNoteName(note)}{NoteUtilities.GetNoteOctave(note)}"
				.Replace("Sharp", "#");
		}

		private void GenerateNewNote()
		{
			int minNote = NoteUtilities.GetNoteNumber(NoteName.C, 2);
			int maxNote = NoteUtilities.GetNoteNumber(NoteName.C, 6);

			// Range 60 (Middle C) to 72 (C5)
			int prev = _targetMidiNote;
			while (prev == _targetMidiNote)
			{
				_targetMidiNote = _random.Next(minNote, maxNote + 1);
			}

			_targetBlackKeyType = _random.NextSingle() > 0.5 ? BlackKeyType.Flat : BlackKeyType.Sharp;

			// We use Dispatcher because this might be called from the MIDI thread
			Dispatcher.Invoke(() =>
			{
				NoteDisplay.Text =
					$"Play MIDI Note: {GetNoteName(_targetMidiNote, _targetBlackKeyType)}";
				StatusDisplay.Text = "Waiting for input...";
				StatusDisplay.Foreground = System.Windows.Media.Brushes.Gray;
				RedrawCanvas();
			});
		}

		private void OnMidiEventReceived(object? sender, MidiEventReceivedEventArgs e)
		{
			if (e.Event is NoteOnEvent noteOn)
			{
				int playedNote = noteOn.NoteNumber;

				// IMPORTANT: UI updates must happen on the UI Thread
				Dispatcher.Invoke(() => { CheckNote(playedNote); });
			}
		}

		private void CheckNote(int playedNote)
		{
			if (playedNote == _targetMidiNote)
			{
				StatusDisplay.Text = "Correct! Well done.";
				StatusDisplay.Foreground = System.Windows.Media.Brushes.Green;

				// Wait a moment then show next note (simplified here)
				GenerateNewNote();
			}
			else
			{
				StatusDisplay.Text = $"Wrong! You played {GetNoteName(playedNote, BlackKeyType.Sharp)}. Try again!";
				StatusDisplay.Foreground = System.Windows.Media.Brushes.Red;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			_inputDevice?.Dispose();
			base.OnClosed(e);
		}

		private void SkipButton_OnClick(object sender, RoutedEventArgs e)
		{
			GenerateNewNote();
		}

		private void RepeatButton_OnClick(object sender, RoutedEventArgs e)
		{
			RedrawCanvas();
		}
	}
}