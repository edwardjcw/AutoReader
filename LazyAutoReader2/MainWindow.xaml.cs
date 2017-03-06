using System;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Documents;


namespace LazyAutoReader2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial  class MainWindow: IDisposable
    {
        private readonly SpeechSynthesizer m_Synth;

        public MainWindow()
        {       
            InitializeComponent();
            DataObject.AddPastingHandler(m_TextBox, OnPaste);
            
            
            m_Synth = new SpeechSynthesizer();
            m_Synth.SetOutputToDefaultAudioDevice();
            m_Synth.StateChanged += SynthOnStateChanged;
            m_Information.Content = "Ready.";
            
        }

        private void Play()
        {
            if (m_Synth.State != SynthesizerState.Ready) return;

            var text = new TextRange(m_TextBox.Document.ContentStart, m_TextBox.Document.ContentEnd).Text;
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
                    m_Information.Content = "Ready.";
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
            if (m_ToggleSwitch.IsChecked == null || !m_ToggleSwitch.IsChecked.Value) return;

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
