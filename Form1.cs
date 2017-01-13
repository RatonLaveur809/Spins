using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;                                 //для файлов
using Microsoft.JScript;
using Microsoft.JScript.Vsa;
using Microsoft;
using SpeechLib;
using System.Speech.Synthesis;

namespace spins
{
    public partial class Form1 : Form
    {
        SpeechVoiceSpeakFlags Async = SpeechVoiceSpeakFlags.SVSFlagsAsync;
        SpeechVoiceSpeakFlags Sync = SpeechVoiceSpeakFlags.SVSFDefault;
        SpeechVoiceSpeakFlags Cancel = SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak;
        SpVoice speech = new SpVoice();

        DataGridView dgv=new DataGridView();

        string savepath;
        int colCount;

        public Form1()
        {
            InitializeComponent();
            List<string> strings = new List<string>();
            string[] s_points;//параметры как строки
            double[,] points; //параметры    
            int kol = 0; //количество строк в файле (и в матрице, собсна)

            SpeechSynthesizer speaker = new SpeechSynthesizer();
            speech.Rate = 0;
            speech.Volume = 100;
            setvoice();

            /////////хш-хш

            opnfldlg.ShowDialog();

            savepath = Path.GetDirectoryName(opnfldlg.FileName);//куды потом сохранять            

            string fullpath = Path.GetFullPath(opnfldlg.FileName);
            tb1.Text = fullpath;
            FileStream file = new FileStream(fullpath, FileMode.Open);
            StreamReader reader = new StreamReader(file);

            while (reader.Peek() >= 0)
            {
                strings.Add(reader.ReadLine());  //строки из файла в strings
                kol++;
            }
            reader.Close();
            file.Close();

            int n = 4;//количество столбцов
            
            points = new double[kol, n];

            //здесь цикл  по всем strings, типа запись в матрицу points
            int q = 0;
            string[] sep = { ",", " " };
            object obj;

            while (q < strings.Count)
            {
                s_points = strings[q].Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < s_points.Length; i++)
                {
                    if (s_points[i] == "?") points[q, i] = -1;//типа не дуга
                    else
                    {
                        obj = Microsoft.JScript.Eval.JScriptEvaluate(s_points[i], VsaEngine.CreateEngine());
                        points[q, i] = (int)obj;
                    }
                }
                q++;
            }

            //считывание времени из history и запись его в points
            strings.Clear();
            fullpath = Path.GetDirectoryName(opnfldlg.FileName);
            fullpath += "/history.csv";
            file = new FileStream(fullpath, FileMode.Open);
            reader = new StreamReader(file);

            while (reader.Peek() >= 0)
            {
                strings.Add(reader.ReadLine());
            }
            reader.Close();
            file.Close();

            q = 1;//строка с подписями не нужна           
            while (q < strings.Count)
            {
                s_points = strings[q].Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
                obj = Microsoft.JScript.Eval.JScriptEvaluate(s_points[0], VsaEngine.CreateEngine());
                points[q - 1, 2] = (double)obj;
                q++;
            }

            //вывод результатов
            // dgv = new DataGridView()
            //{
            colCount =n;
            dgv.ColumnCount = colCount;
            dgv.RowCount = kol;
            dgv.Font = new Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dgv.Location = new Point(10, 20);
            dgv.AutoSize = false;
            dgv.Width = this.Width;
            dgv.Height = this.Height - 100;
            dgv.BackColor = System.Drawing.Color.AntiqueWhite;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgv.EditMode = DataGridViewEditMode.EditProgrammatically;            

            //};
            Controls.Add(dgv);
            dgv.CellEnter += new DataGridViewCellEventHandler(dgv_CellEnter);
            dgv.RowEnter += new DataGridViewCellEventHandler(dgv_CellEnter);
            dgv.CellFormatting += new DataGridViewCellFormattingEventHandler(dgv_CellFormatting);
            dgv.CellPainting += new DataGridViewCellPaintingEventHandler(dgv_CellPainting);

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dgv.Columns[0].HeaderText = "Индекс";
            dgv.Columns[1].HeaderText = "Дуга";
            dgv.Columns[2].HeaderText = "Начало / Окончание";
            dgv.Columns[3].HeaderText = "Продолжительность";

            //индексы и номера дуг           
            for (int i = 0; i < kol; i++)
            {
                dgv[0, i].Value = points[i,0];
                if (points[i, 1] != -1)
                    dgv[1, i].Value = points[i, 1];
                else
                    dgv[1, i].Value = "?";
            }
            //время начала и окончания дуг
            double num = 0;
            int ind=0;
            dgv[2, 0].Value = points[0, 2];
            while(points[ind,1]==num)//ищем конец первой дуги вроде
            {
                ind++;
            }
            dgv[3, 0].Value = Math.Round(points[ind, 2] - points[0, 2],3); 
            for (int i = 1; i < kol; i++)
            {
                if (points[i, 1] != num) 
                {
                    if (points[i - 1, 1] != -1)
                    {
                        dgv[2, i - 1].Value = Math.Round(points[i - 1, 2], 3);
                        dgv[2, i - 1].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                    num = points[i, 1];
                    if (points[i, 1] != -1)
                    {
                        dgv[2, i].Value = Math.Round(points[i, 2],3);
                        dgv[2, i - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                        //продолжительности
                        ind=i;
                        while(points[ind,1]==num)
                        {
                          ind++;
                        }
                        dgv[3, i].Value = Math.Round(points[ind, 2] - points[i, 2],3);                        
                    }
                }
            }
            

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Выберите файл spins.csv", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void setvoice()
        {
            FileStream file = new FileStream(Application.StartupPath + "\\SelectedVoice.txt", FileMode.Open);
            StreamReader reader = new StreamReader(file);
            string currvoicename = reader.ReadLine();
            reader.Close();
            file.Close();

            ISpeechObjectTokens Sotc = speech.GetVoices("", "");
            int n = 0;
            foreach (ISpeechObjectToken Sot in Sotc)
            {
                string tokenname = Sot.GetDescription(0);
                if (tokenname == currvoicename)
                {
                    speech.Voice = Sotc.Item(n);
                }
                n++;
            }
        }

        private void dgv_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            //speech.Speak("",Cancel);
            //if (dgv[colCount - 1, e.RowIndex].Value != null)
            //    speech.Speak("Двигаюсь к точке номер " + dgv[colCount-1, e.RowIndex].Value.ToString(), Async);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string s = "history";
            int ind = savepath.IndexOf(s) + 8;
            s = savepath.Substring(ind);
            ind = s.IndexOf('_');
            int ind2=s.IndexOf('.');
            string s2=s.Substring(ind+1,ind2-ind-1);
            s = s.Remove(ind);

            File.Delete(savepath + "\\" + s + "_spins_" + s2 + ".csv");
            var sw = new StreamWriter(savepath+"\\"+s+"_spins_"+s2+".csv", true, Encoding.Default);
            
            foreach (DataGridViewColumn column in dgv.Columns)//записываем шапку
            {
                sw.Write(column.HeaderText+";");
            }
            sw.WriteLine();
                       
            foreach (DataGridViewRow row in dgv.Rows)
                if (!row.IsNewRow)
                {
                    var first = true;
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (!first) sw.Write(";");
                        if (cell.Value != null)
                        sw.Write(cell.Value.ToString());
                        else sw.Write(";");
                        first = false;
                    }
                    sw.WriteLine();
                }
            sw.Close(); 
        }

        private void dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {

        }

        private void dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

        }
    }
}
//усё!

//opnfldlg.SafeFileName - имя ф-ла с расширением
// MessageBox.Show(s_points[0], "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

//foreach (String str in s_points)
//    tb1.Text += str + Environment.NewLine;

//points[q-1,i-1] = Convert.ToDouble(s_points[i]);

//просто вывод rezults в текстбокс
//int k = 1;
//for (int i = 0; i < (n - 2) / 2; i++)
//{
//    tb2.Text += "di" + k.ToString() + "  " + "de" + k.ToString() + "  ";
//    k++;
//}            
//tb2.Text += "abe  cam  №т." + Environment.NewLine; 
//for (int i = 0; i < raz; i++)
//{
//    for (int j = 0; j < n + 1; j++)
//        tb2.Text += rezults[i, j].ToString()+"      ";
//    tb2.Text += Environment.NewLine;
//}


