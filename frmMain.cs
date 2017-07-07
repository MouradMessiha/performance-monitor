using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;


namespace PerformanceMonitor
{
    public partial class frmMain : Form
    {
        private Bitmap mobjFormBitmap;
        private Graphics mobjBitmapGraphics;
        private int mintFormWidth;
        private int mintFormHeight;
        private Boolean mblnDoneOnce = false;
        private bool mblnClosing = false;

        Process[] marrProcesses;
        List<clsProcess> mlstProcesses = new List<clsProcess>();
        DateTime dteLastSampleTime;
        DateTime dteCurrentSampleTime;
        TimeSpan spnSampleSpan;

        public frmMain()
        {
            InitializeComponent();
        }


        private void frmMain_Activated(object sender, EventArgs e)
        {
            if (!mblnDoneOnce)
            {
                mblnDoneOnce = true;
                mintFormWidth = this.Width;
                mintFormHeight = this.Height; 
                mobjFormBitmap = new Bitmap(mintFormWidth, mintFormHeight, this.CreateGraphics());
                mobjBitmapGraphics = Graphics.FromImage(mobjFormBitmap);

                while (!mblnClosing)
                {
                    TakePerformanceSnapShot();
                    RefreshDisplay();
                    Application.DoEvents();
                    for (int intTimeCounter = 0; intTimeCounter < 10; intTimeCounter++)
                    {
                        Thread.Sleep(100);
                        Application.DoEvents();
                        if (mblnClosing)
                            break;
                    }
                }
            }
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                mintFormWidth = this.Width;
                mintFormHeight = this.Height;
                mobjFormBitmap = new Bitmap(mintFormWidth, mintFormHeight, this.CreateGraphics());
                mobjBitmapGraphics = Graphics.FromImage(mobjFormBitmap);

                RefreshDisplay();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //Do nothing
        }

        private void frmMain_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(mobjFormBitmap, 0, 0);
        }

        private void RefreshDisplay()
        {
            Font objFont;
            SolidBrush objBrushBackground;
            SolidBrush objBrushBlock;
            SolidBrush objBrushBlockActive;
            SolidBrush objBrushFontActive;
            string strLine;
            SizeF objTextSize;
            string strCPUPercentage;

            int intX1;
            int intY1;
            int intX2;
            int intY2;
            int intBlockWidth;
            int intBlockHeight;

            objFont = new Font("MS Sans Serif", 10, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            
            objBrushBackground = new SolidBrush(Color.FromArgb(20,20,90));
            mobjBitmapGraphics.FillRectangle(objBrushBackground, 0, 0, mintFormWidth, mintFormHeight);
            
            objBrushBlock = new SolidBrush(Color.FromArgb(20,20,40));
            objBrushBlockActive = new SolidBrush(Color.FromArgb(100,100,100));
            objBrushFontActive = new SolidBrush(Color.FromArgb(155, 155, 155));
            intBlockWidth = 390;
            intBlockHeight = 20;

            intX1 = 10;
            intY1 = 10;
            intX2 = intX1 + intBlockWidth;
            intY2 = intY1 + intBlockHeight;

            for (int intProcessCounter = 0; intProcessCounter < marrProcesses.Length; intProcessCounter++)
            {
                strCPUPercentage = ProcessCPUPercentage(intProcessCounter, spnSampleSpan);
                strLine = (intProcessCounter.ToString() + "   ").Substring(0, 3) + ":  " +
                    (marrProcesses[intProcessCounter].Id.ToString() + "     ").Substring(0, 5) +
                    ProcessName(intProcessCounter) + "   " + ProcessMemory(intProcessCounter) +
                    strCPUPercentage;
                objTextSize = mobjBitmapGraphics.MeasureString(strLine, objFont, 10000);
                if (strCPUPercentage.Trim() == "0 %CPU" | strCPUPercentage.Trim() == "-")
                {
                    mobjBitmapGraphics.FillRectangle(objBrushBlock, intX1, intY1, intBlockWidth, intBlockHeight);
                    mobjBitmapGraphics.DrawString(strLine, objFont, Brushes.White, intX1 + 10, (intY1 + intY2) / 2 - objTextSize.Height / 2);
                }
                else
                {
                    mobjBitmapGraphics.FillRectangle(objBrushBlockActive, intX1, intY1, intBlockWidth, intBlockHeight);
                    mobjBitmapGraphics.DrawString(strLine, objFont, Brushes.White, intX1 + 10, (intY1 + intY2) / 2 - objTextSize.Height / 2);
                }

                mobjBitmapGraphics.DrawLine(Pens.Gray, intX1 + 4, intY1, intX2 - 4, intY1);
                mobjBitmapGraphics.DrawArc(Pens.Gray, intX2 - 8, intY1, 8, 8, 270, 90);
                mobjBitmapGraphics.DrawLine(Pens.Gray, intX2, intY1 + 4, intX2, intY2 - 4);
                mobjBitmapGraphics.DrawArc(Pens.Gray, intX2 - 8, intY2 - 8, 8, 8, 0, 45);
                mobjBitmapGraphics.DrawArc(Pens.Gray, intX1 , intY1, 8, 8, 225, 45);

                intY1 = intY2 + 10;
                intY2 = intY1 + intBlockHeight;

                if (intY2 > mintFormHeight - 40)
                {
                    intX1 = intX2 + 10;
                    intY1 = 10;
                    intX2 = intX1 + intBlockWidth;
                    intY2 = intY1 + intBlockHeight;
                }
            }

            this.Invalidate();
        }


        private void TakePerformanceSnapShot()
        {
            marrProcesses = Process.GetProcesses();
            dteLastSampleTime = dteCurrentSampleTime;
            dteCurrentSampleTime = DateTime.Now;
            if (dteLastSampleTime == DateTime.MinValue)
                spnSampleSpan = TimeSpan.FromTicks(0);
            else
                spnSampleSpan = dteCurrentSampleTime - dteLastSampleTime;
            for (int intProcessCounter = 0; intProcessCounter < marrProcesses.Length; intProcessCounter++)
                UpdateProcessTime(intProcessCounter);
        }

        private string ProcessMemory(int pintProcessIndex)
        {
            try
            {
                return (String.Format("{0:0,0}", marrProcesses[pintProcessIndex].PeakWorkingSet64/1024) + " KB           ").Substring(0, 14);
            }
            catch
            {
                return ("-                                                                                     ").Substring(0, 112);
            }
        }

        private string ProcessName(int pintProcessIndex)
        {
            try
            {
                return (marrProcesses[pintProcessIndex].ProcessName + "                                     ").Substring(0,23);
            }
            catch
            {
                return "-                                                                 ".Substring(0,23);
            }
        }

        private string ProcessCPUPercentage(int pintProcessIndex, TimeSpan pspnTotalSampleTime)
        {
            double dblProcessTime;
            double dblTotalSampleTime;

            try
            {
                foreach (clsProcess objProcess in mlstProcesses)
                    if (objProcess.ProcessID == marrProcesses[pintProcessIndex].Id)
                        if (pspnTotalSampleTime == TimeSpan.FromTicks(0))
                            return "0 %CPU     ".Substring(0, 11);
                        else
                        {   
                            dblProcessTime = objProcess.ProcessorTime.Seconds + ((double)objProcess.ProcessorTime.Milliseconds) / 1000;
                            dblTotalSampleTime = pspnTotalSampleTime.Seconds + ((double)pspnTotalSampleTime.Milliseconds) / 1000;
                            return (Math.Round(dblProcessTime * 100 / dblTotalSampleTime, 2).ToString() + " %CPU            ").Substring(0, 11);
                        }
            }
            catch
            {
            }
            return "-          ".Substring(0,11);
        }

        private void UpdateProcessTime(int pintProcessIndex)
        {
            bool blnFound = false;

            try
            {
                foreach (clsProcess objProcess in mlstProcesses)
                    if (objProcess.ProcessID == marrProcesses[pintProcessIndex].Id)
                    {
                        objProcess.ProcessorTime = marrProcesses[pintProcessIndex].TotalProcessorTime - objProcess.TotalProcessorTime;
                        objProcess.TotalProcessorTime = marrProcesses[pintProcessIndex].TotalProcessorTime;
                        blnFound = true;
                    }
                if (!blnFound)
                    mlstProcesses.Add(new clsProcess(marrProcesses[pintProcessIndex].Id, marrProcesses[pintProcessIndex].TotalProcessorTime));
            }
            catch
            {
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            mblnClosing = true;
        }
    }

    public class clsProcess
    {
        private int mintProcessID;
        private TimeSpan mspnTotalProcessorTime;
        private TimeSpan mspnProcessorTime;

        public clsProcess(int pintProcessID, TimeSpan pspnTotalProcessorTime)
        {
            mintProcessID = pintProcessID;
            mspnTotalProcessorTime = pspnTotalProcessorTime;
            mspnProcessorTime = TimeSpan.FromTicks(0);
        }

        public int ProcessID
        {
            get
            {
                return mintProcessID;
            }
            set
            {
                mintProcessID = value;
            }
        }

        public TimeSpan TotalProcessorTime
        {
            get
            {
                return mspnTotalProcessorTime;
            }
            set
            {
                mspnTotalProcessorTime = value;
            }
        }

        public TimeSpan ProcessorTime
        {
            get
            {
                return mspnProcessorTime;
            }
            set
            {
                mspnProcessorTime = value;
            }
        }
    }
}
