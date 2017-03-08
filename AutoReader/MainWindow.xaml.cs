using System;
using System.Windows;
using System.Windows.Documents;
using System.Speech.Synthesis;
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

            m_Synth = new SpeechSynthesizer();
            m_Synth.SetOutputToDefaultAudioDevice();

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

        private void SynthOnStateChangedStartup(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            switch (stateChangedEventArgs.State)
            {
                case SynthesizerState.Speaking:
                    m_Information.Content = "Speech in Process";
                    m_Button.Content = "Stop";
                    
                    return;
                case SynthesizerState.Paused:
                    break;
                case SynthesizerState.Ready:
                    m_Information.Content = "Done. Ready.";
                    m_Button.Content = "Start";
                    Application.Current.Shutdown();
                    break;
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
                    m_Button.Content = "Stop";
                    return;
                case SynthesizerState.Paused:
                    break;
                case SynthesizerState.Ready:
                    m_Information.Content = "Done. Ready.";
                    m_Button.Content = "Start";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (m_Synth.State != SynthesizerState.Ready)
            {
                m_Synth.SpeakAsyncCancelAll();
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
    }
}
