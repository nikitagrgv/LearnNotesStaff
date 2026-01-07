using System.Windows;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System;

namespace LearnNotesStaff
{
	public partial class MainWindow : Window
	{
		private InputDevice _inputDevice;
		private int _targetMidiNote;
		private Random _random = new Random();

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