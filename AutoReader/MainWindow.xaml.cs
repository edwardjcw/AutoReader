using System;
using System.Windows;
using System.Windows.Documents;
using System.Speech.Synthesis;
using System.Windows.Media;
using Microsoft.Win32;

namespace AutoReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IDisposable
    {

        private readonly SpeechSynthesizer m_Synth;

        public MainWindow()
        {
            InitializeComponent();
            DataObject.AddPastingHandler(m_TextBox, OnPaste);

            m_TextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 100, 200));
           
            m_Synth = new SpeechSynthesizer();

            ModifyLexicon();

            m_Synth.SetOutputToDefaultAudioDevice();
            m_Synth.SpeakProgress += OnSpeakProgress;

            if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "1")
            {
                m_AutoPlayToggle.Visibility = Visibility.Hidden;
                m_AutoSaveToggle.Visibility = Visibility.Hidden;
                m_PasteButton.Visibility = Visibility.Hidden;
                m_TextBox.IsEnabled = false;
                m_Synth.StateChanged += SynthOnStateChangedStartup;
                OnClickClearAndPaste(this, new RoutedEventArgs());
            }

            m_Synth.StateChanged += SynthOnStateChanged;
            m_Information.Content = "Ready.";

        }

        private void ModifyLexicon()
        {
            m_Synth.AddLexicon(new Uri(@"pack://application:,,,/lexicon.pls"), "application/pls+xml");
        }

        private static TextPointer EndTextPoint(FlowDocument document, int startPosition, string end)
        {
            var offset = 1;
            TextPointer startPointer = document.ContentStart.GetPositionAtOffset(startPosition);

            if (startPointer == document.ContentEnd)
            {
                return null;
            }

            while (true)
            {
                TextPointer endPointer = document.ContentStart.GetPositionAtOffset(startPosition + offset);
                var text = new TextRange(startPointer, endPointer).Text;

                if (text.EndsWith(end))
                {
                    return document.ContentStart.GetPositionAtOffset(startPosition + (offset - 1));
                }

                offset = offset + 1;
            }
        }

        private void OnSpeakProgress(object sender, SpeakProgressEventArgs speakProgressEventArgs)
        {
            var startPosition = speakProgressEventArgs.CharacterPosition;
            TextPointer startPointer = StartPointer(startPosition, m_TextBox.Document.ContentStart, 0);

            if (startPointer == null)
            {
                return;
            }

            WordBreaker.GetWordRange(startPointer).ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            m_TextBox.Focus();
        }

        private static TextPointer StartPointer(int startPosition, TextPointer text, int charactersPassed)
        {
            while (true)
            {
                if (text == null)
                {
                    return null;
                }

                if (text.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    var contextSize = text.GetTextRunLength(LogicalDirection.Forward);
                    if (charactersPassed + contextSize <= startPosition)
                    {
                        text = text.GetNextContextPosition(LogicalDirection.Forward);
                        charactersPassed = charactersPassed + contextSize;
                        continue;
                    }

                    var relativeStart = startPosition - charactersPassed;
                    return text.GetPositionAtOffset(relativeStart);
                }

                text = text.GetNextContextPosition(LogicalDirection.Forward);
            }
        }


        private void SynthOnStateChangedStartup(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            switch (stateChangedEventArgs.State)
            {
                case SynthesizerState.Speaking:
                    m_Information.Content = "Speech in Process";
                    m_PlayButton.Content = "Stop";
                    m_PauseButton.Content = "Pause";
                    m_PauseButton.IsEnabled = true;
                    return;
                case SynthesizerState.Paused:
                    m_PauseButton.Content = "Resume";
                    return;
                case SynthesizerState.Ready:
                    Application.Current.Shutdown();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Play()
        {
            if (m_Synth.State != SynthesizerState.Ready) return;

            var text = new TextRange(m_TextBox.Document.ContentStart, m_TextBox.Document.ContentEnd).Text;
            m_Synth.SetOutputToDefaultAudioDevice();

            if (m_AutoSaveToggle.IsChecked != null && m_AutoSaveToggle.IsChecked.Value)
            {
                var saveFileDialog = new SaveFileDialog { Title = "Select location to save file." };
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName == "")
                {
                    m_Information.Content = "No file selected.";
                    return;
                }
                m_Synth.SetOutputToWaveFile(saveFileDialog.FileName);
                m_Information.Content = "File save complete";
            }

            m_Synth.SpeakAsync(text);
        }

        private void SynthOnStateChanged(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            switch (stateChangedEventArgs.State)
            {
                case SynthesizerState.Speaking:
                    m_Information.Content = "Speech in Process";
                    m_PlayButton.Content = "Stop";
                    m_PauseButton.Content = "Pause";
                    m_PauseButton.IsEnabled = true;
                    return;
                case SynthesizerState.Paused:
                    m_PauseButton.Content = "Resume";
                    return;
                case SynthesizerState.Ready:
                    m_Information.Content = "Done. Ready.";
                    m_PlayButton.Content = "Start";
                    m_PauseButton.Content = "Pause";
                    m_PauseButton.IsEnabled = false;
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            if (m_Synth.State == SynthesizerState.Paused)
            {
                m_Synth.Resume();
            }

            if (m_Synth.State != SynthesizerState.Ready)
            {
                m_Synth.SpeakAsyncCancelAll();
                m_Information.Content = "Done. Ready.";
                m_PlayButton.Content = "Start";
                m_PauseButton.Content = "Pause";
                m_PauseButton.IsEnabled = false;
                return;
            }

            Play();
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (m_AutoPlayToggle.IsChecked == null || !m_AutoPlayToggle.IsChecked.Value) return;

            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (text != null)
                {
                    m_TextBox.Document.Blocks.Clear();
                    m_TextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                }
            }

            e.CancelCommand();
            Play();
        }

        private void OnClickClearAndPaste(object sender, RoutedEventArgs e)
        {
            if (m_Synth.State != SynthesizerState.Ready)
            {
                m_Synth.SpeakAsyncCancelAll();
            }

            m_TextBox.Document.Blocks.Clear();
            m_TextBox.Paste();
            Play();
        }

        public void Dispose()
        {
            m_Synth.Dispose();
        }

        private void OnPauseClick(object sender, RoutedEventArgs e)
        {
            switch (m_Synth.State)
            {
                case SynthesizerState.Speaking:
                    m_Synth.Pause();
                    return;
                case SynthesizerState.Paused:
                    m_Synth.Resume();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
