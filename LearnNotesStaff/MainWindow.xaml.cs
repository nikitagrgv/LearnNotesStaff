using System.Windows;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LearnNotesStaff
{
	public partial class MainWindow : Window
	{
		private InputDevice _inputDevice;
		private int _targetMidiNote;
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
			ClearCanvas();
			DrawStaff();
			DrawNote(64); // Draw an 'E' (bottom line of treble staff)
		}

		private void ClearCanvas()
		{
			StaffCanvas.Children.Clear();
		}

		private void DrawStaff()
		{
			for (int i = 0; i < 5; i++)
			{
				Line line = new Line
				{
					X1 = -100,
					X2 = 100,
					Y1 = StaffTop + (i * LineSpacing),
					Y2 = StaffTop + (i * LineSpacing),
					Stroke = Brushes.Black,
					StrokeThickness = 2
				};
				StaffCanvas.Children.Add(line);
			}
		}

		private void DrawNote(int midiNumber)
		{
			// 1. Calculate the vertical position
			// MIDI 64 (E4) is the bottom line. Let's map it there.
			// Every 1 MIDI note change is 0.5 * LineSpacing (roughly, ignoring sharps)
			double y = StaffTop + (4 * LineSpacing) - ((midiNumber - 64) * (LineSpacing / 2));

			// 2. Create the circle (the note head)
			Ellipse noteHead = new Ellipse
			{
				Width = 20,
				Height = 15, // Notes are slightly oval
				Fill = Brushes.Black
			};

			// 3. Position it on the Canvas
			Canvas.SetLeft(noteHead, 0);
			Canvas.SetTop(noteHead, y - (noteHead.Height / 2));

			StaffCanvas.Children.Add(noteHead);
		}

		private void GenerateNewNote()
		{
			// Range 60 (Middle C) to 72 (C5)
			_targetMidiNote = _random.Next(60, 73);

			// We use Dispatcher because this might be called from the MIDI thread
			Dispatcher.Invoke(() =>
			{
				NoteDisplay.Text = $"Play MIDI Note: {_targetMidiNote}";
				StatusDisplay.Text = "Waiting for input...";
				StatusDisplay.Foreground = System.Windows.Media.Brushes.Gray;
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
				StatusDisplay.Text = $"Wrong! You played {playedNote}. Try again!";
				StatusDisplay.Foreground = System.Windows.Media.Brushes.Red;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			_inputDevice?.Dispose();
			base.OnClosed(e);
		}
	}
}