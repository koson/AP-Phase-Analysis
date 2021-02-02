using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AP_Phase_Analysis
{
    public partial class Form1 : Form
    {
        const int SAMPLE_RATE = 20_000;

        double[] V;
        double[] DVDT;
        int MouseIndex;
        ScottPlot.Plottable.ScatterPlot Sp;
        ScottPlot.Plottable.ScatterPlot Highlight;
        ScottPlot.Plottable.VLine Vline1;
        ScottPlot.Plottable.VLine Vline2;

        public Form1()
        {
            InitializeComponent();
            formsPlot1.Plot.Style(ScottPlot.Style.Control);
            formsPlot2.Plot.Style(ScottPlot.Style.Control);
            formsPlot3.Plot.Style(ScottPlot.Style.Control);
            formsPlot1.Configuration.Quality = ScottPlot.Control.QualityMode.Low;
            formsPlot2.Configuration.Quality = ScottPlot.Control.QualityMode.Low;
            formsPlot3.Configuration.Quality = ScottPlot.Control.QualityMode.Low;

            formsPlot1.AxesChanged += FormsPlot1_AxesChanged;
            formsPlot2.AxesChanged += FormsPlot2_AxesChanged;
            formsPlot1.MouseMove += FormsPlot1_MouseMove;
            formsPlot2.MouseMove += FormsPlot2_MouseMove;
        }

        private void FormsPlot2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None)
                return;
            (double x, _) = formsPlot2.GetMouseCoordinates();
            MouseIndex = ArrayIndexForX(x);
            Vline1.X = (double)MouseIndex / SAMPLE_RATE;
            Vline2.X = Vline1.X;
            formsPlot1.Render(skipIfCurrentlyRendering: true);
            formsPlot2.Render(skipIfCurrentlyRendering: true);
            UpdatePhasePlot();
        }

        private void FormsPlot1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None)
                return;
            (double x, _) = formsPlot1.GetMouseCoordinates();
            MouseIndex = ArrayIndexForX(x);
            Vline1.X = (double)MouseIndex / SAMPLE_RATE;
            Vline2.X = Vline1.X;
            formsPlot1.Render(skipIfCurrentlyRendering: true);
            formsPlot2.Render(skipIfCurrentlyRendering: true);
            UpdatePhasePlot();
        }

        private void FormsPlot2_AxesChanged(object sender, EventArgs e)
        {
            var limits = formsPlot2.Plot.GetAxisLimits();
            formsPlot1.Plot.SetAxisLimitsX(limits.XMin, limits.XMax);
            formsPlot1.Render(skipIfCurrentlyRendering: true);
            UpdatePhasePlot();
        }

        private void FormsPlot1_AxesChanged(object sender, EventArgs e)
        {
            var limits = formsPlot1.Plot.GetAxisLimits();
            formsPlot2.Plot.SetAxisLimitsX(limits.XMin, limits.XMax);
            formsPlot2.Render(skipIfCurrentlyRendering: true);
            UpdatePhasePlot();
        }

        private int ArrayIndexForX(double coordinateX)
        {
            var index = (int)(coordinateX * SAMPLE_RATE);
            index = Math.Max(0, index);
            index = Math.Min(V.Length - 1, index);
            return index;
        }

        private void UpdatePhasePlot()
        {
            // determine min/max index of the data in view
            var limits = formsPlot1.Plot.GetAxisLimits();
            int min = ArrayIndexForX(limits.XMin);
            int max = ArrayIndexForX(limits.XMax);

            // create V and dVdt arrays for just the data in view
            int segmentSize = max - min;
            double[] xs = new double[segmentSize];
            double[] ys = new double[segmentSize];
            for (int i = 0; i < segmentSize; i++)
            {
                xs[i] = V[i + min];
                ys[i] = DVDT[i + min];
            }

            // update the plot with these small arrays
            Sp.Update(xs, ys);

            // move the highlighted point to where the cursor is
            Highlight.Xs[0] = V[MouseIndex];
            Highlight.Ys[0] = DVDT[MouseIndex];

            formsPlot3.Render(lowQuality: xs.Length > 5000, skipIfCurrentlyRendering: true);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string defaultAbfPath = @"C:\Users\scott\Documents\GitHub\pyABF\data\abfs\17o05027_ic_ramp.abf";
            if (File.Exists(defaultAbfPath))
                LoadABF(defaultAbfPath);
        }

        private void LoadABF(string path)
        {
            // setup the voltage plot
            path = Path.GetFullPath(path);
            lblPath.Text = path;
            var abf = new AbfSharp.ABF(path);
            V = abf.ReadAllSweeps();
            formsPlot1.Plot.AddSignal(V, SAMPLE_RATE, Color.Blue);
            Vline1 = formsPlot1.Plot.AddVerticalLine(0, Color.Black, style: ScottPlot.LineStyle.Dash);
            formsPlot1.Plot.AxisAuto(horizontalMargin: 0);
            formsPlot1.Plot.YLabel("mV");

            // setup the dVdt plot
            DVDT = new double[V.Length];
            for (int i = 1; i < V.Length; i++)
                DVDT[i] = (V[i] - V[i - 1]) * (SAMPLE_RATE / 1000);
            DVDT[0] = DVDT[1];
            formsPlot2.Plot.AddSignal(DVDT, SAMPLE_RATE, Color.Red);
            Vline2 = formsPlot2.Plot.AddVerticalLine(0, Color.Black, style: ScottPlot.LineStyle.Dash);
            formsPlot2.Plot.AxisAuto(horizontalMargin: 0);
            formsPlot2.Plot.YLabel("mV/ms");

            // setup the phase plot
            Sp = formsPlot3.Plot.AddScatterLines(V, DVDT, Color.Gray);
            Highlight = formsPlot3.Plot.AddPoint(0, 0, Color.Black, 10, ScottPlot.MarkerShape.filledCircle);
            formsPlot3.Render(lowQuality: true);
            formsPlot3.Plot.XLabel("mV");
            formsPlot3.Plot.YLabel("mV/ms");
        }
    }
}
