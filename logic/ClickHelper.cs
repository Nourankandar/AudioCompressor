using System;
using System.Windows.Forms;

namespace AudioCompressor.logic
{
    public class ClickHelper
    {
        private readonly AudioManager audioManager;

        public ClickHelper(AudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        // التقاط كليك زر الـ Browse
        public void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    audioManager.LoadSelectedFile(openFileDialog.FileName);
                }
            }
        }

        // التقاط كليك زر الـ Play
        public void BtnPlay_Click(object sender, EventArgs e)
        {
            audioManager.TogglePlay();
        }

        // التقاط كليك زر الـ Reset
        public void BtnReset_Click(object sender, EventArgs e)
        {
            audioManager.ResetAudio();
        }

        // التقاط أحداث السحب والإفلات وتمريرها
        public void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                audioManager.LoadSelectedFile(files[0]);
            }
        }

        public void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }
        public void BtnSave_Click(object sender, EventArgs e)
        {   
            if (!audioManager.IsFileLoaded)
            {
                MessageBox.Show("There is no file to save. Please load or compress an audio file first.", 
                                "No File Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma";
                saveFileDialog.Title = "Save Compressed File";
                
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // استدعاء تابع الحفظ من الـ audioManager
                    audioManager.SaveAudioFile(saveFileDialog.FileName);
                }
            }
        }

    }
}