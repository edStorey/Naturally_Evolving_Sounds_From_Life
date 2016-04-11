using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Midi;



namespace TestRect2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Region : Static Variables
        // global variables
        static int SquaresAcross = 18;
        static int SquaresDown = 10;
        static Canvas baseCanvas = new Canvas();
        static Window mainWindow;
        static Pitch[] Cmajor, Aminor;
        static int stop = 0, right = 0, left = 0, stap = 0, padThresh = 4, blank = 0, blank2 = 0;
        static int Device = 0;
        static OutputDevice outputDevice = OutputDevice.InstalledDevices[Device];
        static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        static int bottomgen2 = 8, bottomgen3 = 4, bottomgen4 = 2;
        static int topprev = 0, bottomprev = 0, off = 0, first = 0;
        static double bottomav = 0;
        static int UpdateSpeed = 1000;
        static int prevgenstop = 0;
        static int prevgentab = 0;
        static Canvas secondCanvas = new Canvas();
        static Canvas thirdCanvas = new Canvas();
        static Canvas baseCanvasCover = new Canvas();
        static List<OutputDevice> devices = new List<OutputDevice>();
        static List<int> used = new List<int>();
        static int firstPlay = 0;
        static Window winLife;
        static int Movement = 0;
        static int LoopGame = 0;
        static Color LeftColour = Colors.MediumSpringGreen;
        static Color RightColour = Colors.GreenYellow;
        static Color MoveColour = Colors.MediumPurple;
        static double Colx = MoveColour.B;
        static double Coly = MoveColour.G;
        static double Colz = MoveColour.R;
        static int guitPlay = 7;


        //Kinect Variables
        private KinectSensor _KinectDevice;
        private WriteableBitmap _DepthImage;
        private Int32Rect _DepthImageRect;
        private short[] _DepthPixelData;
        private int _DepthImageStride;
        private int _TotalFrames;
        private DateTime _StartFrameTime;
        private const int LoDepthThreshold = 1220;
        private const int HiDepthThreshold = -2000;
        private const int LowThreshold = LoDepthThreshold / 2;
        private const int HighThreshold = (int)(LoDepthThreshold * 1.5);
        static double Timbre;
        static int highestCheck = 0, run = 0, _xfilterindex = 0;
        const int FILTERARRAYLENGTH = 5;
        static double[] lastfewX = new double[FILTERARRAYLENGTH] { 0, 0, 0, 0, 0 };
        static double[] sortingArray = new double[FILTERARRAYLENGTH];
        static double[] lastfewXpos = new double[FILTERARRAYLENGTH] { 0, 0, 0, 0, 0 };
        static double[] sortingArraypos = new double[FILTERARRAYLENGTH];
        static double[] lastfewXx = new double[FILTERARRAYLENGTH] { 0, 0, 0, 0, 0 };
        static double[] lastfewXy = new double[FILTERARRAYLENGTH] { 0, 0, 0, 0, 0 };
        static double[] lastfewXz = new double[FILTERARRAYLENGTH] { 0, 0, 0, 0, 0 };
        static double[] sortingArrayx = new double[FILTERARRAYLENGTH];
        static double[] sortingArrayy = new double[FILTERARRAYLENGTH];
        static double[] sortingArrayz = new double[FILTERARRAYLENGTH];
        static double elevang = 0;
        #endregion

        #region Region : Life Cell
        class cLifeCell
        {
            // Internal rectangle
            internal Rectangle rect;

            // Shows whether teh cell is alive or dead
            // by always equalling 1 or 0
            internal int status;

            // Amount of pixels shown by the kinect 
            // Within the region covered by this cell
            internal int pixel;

            // Shows whether this cell has been created
            // by the game or the Kinect
            internal int body;

            // Constructor setting the initial value of status
            internal cLifeCell(int init)
            {
                status = init;
            }
            
        }
        List<cLifeCell> theGame = new List<cLifeCell>();
        // Array of Cells for 3 generations
        static cLifeCell[,] theCells = null;
        static cLifeCell[,] theCellsgen2 = null;
        static cLifeCell[,] theCellsgen3 = null;
        #endregion

        #region Region : Main
        public MainWindow()
        {
            // Sets mainWindow to equal the current window
            mainWindow = System.Windows.Application.Current.MainWindow;

            // Opens the first MIDI device
            outputDevice.Open();

            // Sets the first two channels to an electric guitar and a chord pad
            outputDevice.SendProgramChange(Channel.Channel1, Instrument.ElectricGuitarJazz);
            outputDevice.SendProgramChange(Channel.Channel2, Instrument.Pad2Warm);

            //sets the initial values for the pan control
            outputDevice.SendControlChange(Channel.Channel1, Midi.Control.Pan, 63);
            outputDevice.SendControlChange(Channel.Channel2, Midi.Control.Pan, 63);

            

            InitializeComponent();

            #region Region : Initialise Canvases
            // Sets the Canvas that all the first generation components will later be added to
            // to the same height and width as the window
            baseCanvas.Height = mainCanvas.Height;
            baseCanvas.Width = mainCanvas.Width;

            // basCanvasCover will lie ontop of all the other canvases wwith transparent
            // rectangles on it that will give the illusion of being able to click 
            //around the visible squares yet still select them
            baseCanvasCover.Height = mainCanvas.Height;
            baseCanvasCover.Width = mainCanvas.Width;


            // Sets the canvas for the second generation to 90% of the baseCanvas size
            // allowing for the rectangles to 90% of the previous generations size
            secondCanvas.Height = mainCanvas.Height * 0.9;
            secondCanvas.Width = mainCanvas.Width * 0.9;

            // As before with the second generation this is 110% of the size
            // this allowed for easier sizing of teh rectangles later on
            thirdCanvas.Height = mainCanvas.Height * 1.1;
            thirdCanvas.Width = mainCanvas.Width * 1.1;
            #endregion

            // Sets up the initial sizes of each array of lifecells
            theCells = new cLifeCell[SquaresDown, SquaresAcross];
            theCellsgen2 = new cLifeCell[SquaresDown, SquaresAcross];
            theCellsgen3 = new cLifeCell[SquaresDown, SquaresAcross];

            #region Region : Set Up Arrays of Life Cells
            for (int loopy = 0; loopy < SquaresDown; loopy++)
            {
                for (int lopy = 0; lopy < SquaresAcross; lopy++)
                {
                    // Stores all the new cells in an x by y array
                    // and sets the initial value of status to 0
                    theCells[loopy, lopy] = new cLifeCell(0);
                    theCellsgen2[loopy, lopy] = new cLifeCell(0);
                    theCellsgen3[loopy, lopy] = new cLifeCell(0);

                }
            }
            #endregion

            // Gets a list of all the potential MIDI devices
            List<Midi.OutputDevice> midi_outs = GetDeviceList();
            // Adds the names to the Lead_Channel ComboBox
            foreach (Midi.OutputDevice outd in midi_outs)
            {
                // Adds all the MIDI devices to the Lead_Channel ComboBox
                Lead_Channel.Items.Add(outd.Name);
            }

            // Sets all the initial values for the interface
            Lead_Channel.SelectedIndex = 0;
           // CellColourBox.SelectedIndex = 0;
            Update.Value = UpdateSpeed;

            // Adds the various methods to their events
            #region Region : Events
            Lead_Channel.SelectionChanged += new SelectionChangedEventHandler(cmbMIDIDevice_SelectionChanged);

            Clear.Click += new RoutedEventHandler(Clear_clicked);

            Start.Click += new RoutedEventHandler(Start_clicked);

            Pause.Click += new RoutedEventHandler(Stop_clicked);

            Random.Click += new RoutedEventHandler(Random_clicked);

            MovementSquares.Checked += new RoutedEventHandler(Movement_Checked);

            MovementSquares.Unchecked += new RoutedEventHandler(Movement_Unchecked);

            LoopBox.Checked += new RoutedEventHandler(Loop_Checked);

            LoopBox.Unchecked += new RoutedEventHandler(Loop_Unchecked);

            Reset.Click += new RoutedEventHandler(Reset_clicked);

            Draw_rectangles_grid(baseCanvas, secondCanvas, thirdCanvas);

            timer.Tick += new EventHandler(GoL);

            AngleSlider.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(Angle_Changed);

            Update.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Speed_Changed);
            #endregion

            // Rescaling method
            InitReScaling(mainCanvas);

            // Sets up the Kinect Device
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);

            // If the window closes, remember to tidy up things first:
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            // Sets the value of AngleSlider to equal the current elevation angle of the Kinect
            // this means that whatever value it had previously been left at will be recognised
            // when the program is loaded
            AngleSlider.Value = elevang;

        }


        // If the mainwindow closes this method is called
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
            // if any MIDI notes have been played then stop all of them
            if (firstPlay > 0)
            {
                outputDevice.SilenceAllNotes();

                outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);
            }

            // stop all timer functions
            timer.Stop();
            timer.Enabled = false;


        }
        #endregion

        #region Region : Left Mouse Button Click
        void rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Finds the x and y coordinates relative to mainCanvas and puts them in context of the
            // lifeCells
            double x = ((e.GetPosition(mainCanvas).X * (SquaresAcross - 2)) / mainCanvas.ActualWidth);
            double y = ((e.GetPosition(mainCanvas).Y * (SquaresDown - 2)) / mainCanvas.ActualHeight);

            // Rounds the x an y variables to their lowest possible value and adds 1
            // the 1 is added so that the values atrt at 1 and end at 16 or 8
            // rather than starting at 0 and ending on 15 or 7
            int x_scaled = (int)(Math.Floor(x)) + 1;
            int y_scaled = (int)(Math.Floor(y)) + 1;

            // If either of the values are greater than either 8 or 16 make them 8 or 16
            if (x_scaled >= 16) x_scaled = 16;
            if (y_scaled >= 8) y_scaled = 8;

            // If the cells are coloured in run this
            if ((theCells[y_scaled, x_scaled].rect.Fill as SolidColorBrush).Color == LeftColour ||
                            (theCells[y_scaled, x_scaled].rect.Fill as SolidColorBrush).Color == RightColour)
            {
                // Changes the colour to transparent
                theCells[y_scaled, x_scaled].rect.Fill = new SolidColorBrush(Colors.Transparent);

                // Sets the value to off and whos that it is not created by the kinect
                theCells[y_scaled, x_scaled].status = 0;
                theCells[y_scaled, x_scaled].body = 0;

            }

            // Else if the colour is transparent run this
            else if ((theCells[y_scaled, x_scaled].rect.Fill as SolidColorBrush).Color == (Brushes.Transparent).Color)
            {
                // If the coordinates are on the left give it the left colour
                if (x_scaled < (SquaresAcross - 2) / 2 + 1) { theCells[y_scaled, x_scaled].rect.Fill = new SolidColorBrush(LeftColour); }
                // If the coordinates are on the right give it teh right colour
                else if (x_scaled > (SquaresAcross - 2) / 2) { theCells[y_scaled, x_scaled].rect.Fill = new SolidColorBrush(RightColour); }

                // Shows that the cell is alive but not created by teh Kinect
                theCells[y_scaled, x_scaled].status = 1;
                theCells[y_scaled, x_scaled].body = 0;

            }


        }
        #endregion

        #region Region: The rescaling stuff
        private Canvas rescaleCanvas;
        private double rescaleDefaultWidth, rescaleDefaultHeight;
        private double rescaleOffsetWidth, rescaleOffsetHeight;
        void InitReScaling(Canvas topCanvas) // Initialises the stuff for changing window sizes
        {
            // These find the actual size of the window area without the boarders
            double ratioWidth = this.Width * 0.975;
            double ratioHeight = this.Height * 0.9333333333333335;

            rescaleCanvas = topCanvas;

            // Finds the size of the boarders
            rescaleOffsetWidth = this.Width - ratioWidth;
            rescaleOffsetHeight = this.Height - ratioHeight;
            rescaleDefaultWidth = ratioWidth;
            rescaleDefaultHeight = ratioHeight;

            // Set the Grid to fill the available window area
            rescaleCanvas.Margin = new Thickness(0, 0, 0, 0);
            rescaleCanvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            rescaleCanvas.VerticalAlignment = VerticalAlignment.Top;

            // And register an event handler for form re-sizing:
            this.SizeChanged += new SizeChangedEventHandler(Window1_SizeChanged);
        }
        void Window1_SizeChanged(object sender, SizeChangedEventArgs e) // When window changes size
        {
            // Rescales every objecton mainCanvas
            double scaleHeight = (this.ActualHeight - rescaleOffsetHeight) / rescaleDefaultHeight;
            double scaleWidth = (this.ActualWidth - rescaleOffsetWidth) / rescaleDefaultWidth;
            ScaleTransform ulla = new ScaleTransform(scaleWidth, scaleHeight);
            rescaleCanvas.RenderTransform = ulla;
        }
        #endregion

        #region Region : Set Up Canvases Rectangles etc.

        // Sets up all the canvases, rectangles and cLifeCell clases
        public void Draw_rectangles_grid(Canvas baseCanvas, Canvas secondCanvas, Canvas thirdCanvas)
        {

            #region Region : Definitions

            // Sets the overall size of the grids
            double e = mainCanvas.Height * 0.8;
            double f = mainCanvas.Width * 0.8;
            double c = mainCanvas.Height * 0.90;
            double d = mainCanvas.Width * 0.90;
            double a = mainCanvas.Height;
            double b = mainCanvas.Width;
            // Sets the height of teh area in which each cell takes up
            double rectHeight = a * 0.975 / (SquaresDown - 2);
            double rectWidth = b * 1.025 / (SquaresAcross - 2);
            double rect2Height = c * 0.975 / (SquaresDown - 2);
            double rect2Width = d * 1.025 / (SquaresAcross - 2);
            double rect3Height = e * 0.975 / (SquaresDown - 2);
            double rect3Width = f * 1.025 / (SquaresAcross - 2);

            // Sets the area of the actual cell
            double actrectHeight = rectHeight * 0.75;
            double actrectWidth = rectWidth * 0.75;
            double actrect2Height = rect2Height * 0.75;
            double actrect2Width = rect2Width * 0.75;
            double actrect3Height = rect3Height * 0.75;
            double actrect3Width = rect3Width * 0.75;

            // Sets the horizontal and vertical Alignments
            mainCanvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            mainCanvas.VerticalAlignment = VerticalAlignment.Center;

            // Sets the opacity level of each canvas The foremost being the least opaque
            // mainCanvas is not opaque due to everything being layered onto it
            baseCanvas.Opacity = 0.9;
            secondCanvas.Opacity = 0.4;
            thirdCanvas.Opacity = 0.2;
            mainCanvas.Opacity = 1;


            ScaleTransform mainScaleTransform = new ScaleTransform();
            mainScaleTransform.ScaleX = 1;
            mainScaleTransform.ScaleY = 1;
            baseCanvas.LayoutTransform = mainScaleTransform;

            #endregion

            #region Region : For loop
            for (int loop = 0; loop < SquaresAcross - 2; loop++)
            {
                for (int lop = 0; lop < SquaresDown - 2; lop++)
                {
                    // Build rectangle of the set height and width
                    Rectangle rect = new Rectangle();
                    rect.Height = actrectHeight;
                    rect.Width = actrectWidth;

                    Rectangle rect2 = new Rectangle();
                    rect2.Height = actrect2Height;
                    rect2.Width = actrect2Width;
                    Rectangle rect3 = new Rectangle();
                    rect3.Height = actrect3Height;
                    rect3.Width = actrect3Width;

                    // An extra layer of rectangles is added as the cells have gaps clicking
                    // adding a transparent cell on top can allow the user to click on a gap
                    // and still produce a cell
                    Rectangle rectCover = new Rectangle();
                    rectCover.Height = rectHeight;
                    rectCover.Width = rectWidth;

                    // Sts the initial colour of all rectangles as being transparent
                    rect.Fill = new SolidColorBrush(Colors.Transparent);
                    rect2.Fill = new SolidColorBrush(Colors.Transparent);
                    rect3.Fill = new SolidColorBrush(Colors.Transparent);
                    rectCover.Fill = new SolidColorBrush(Colors.Transparent);

                    // Sets the position of each rectangle relative to a canvas
                    Canvas.SetTop(rect, rectHeight * lop);
                    Canvas.SetLeft(rect, rectWidth * loop);

                    //sets the postion of the next generations of cells with an indent
                    Canvas.SetTop(rect2, (rect2Height * lop) + (rectWidth * 0.4));
                    Canvas.SetLeft(rect2, (rect2Width * loop) + (rectWidth * 0.8));
                    Canvas.SetTop(rect3, (rect3Height * lop) + (rectWidth * 0.8));
                    Canvas.SetLeft(rect3, (rect3Width * loop) + (rectWidth * 1.6));
                    Canvas.SetTop(rectCover, rectHeight * lop);
                    Canvas.SetLeft(rectCover, rectWidth * loop);

                    // Sets all but the rectCover cells to be visible
                    rect.Visibility = System.Windows.Visibility.Visible;
                    rect2.Visibility = System.Windows.Visibility.Visible;
                    rect3.Visibility = System.Windows.Visibility.Visible;

                    // These add the cells to their respective Canvases
                    // and then their respective cLifeCell class
                    baseCanvasCover.Children.Add(rectCover);
                    baseCanvas.Children.Add(rect);
                    theCells[lop + 1, loop + 1].rect = rect;
                    secondCanvas.Children.Add(rect2);
                    theCellsgen2[lop + 1, loop + 1].rect = rect2;
                    thirdCanvas.Children.Add(rect3);
                    theCellsgen3[lop + 1, loop + 1].rect = rect3;

                    // Adds the rect_MouseLeftButtonDown method to each of the RectCover rectangles
                    rectCover.MouseLeftButtonDown += new MouseButtonEventHandler(rect_MouseLeftButtonDown);

                }
            }
            #endregion

            // Adds each canvas in ascending order Furthest back first closest last
            mainCanvas.Children.Add(thirdCanvas);
            mainCanvas.Children.Add(secondCanvas);
            mainCanvas.Children.Add(baseCanvas);
            mainCanvas.Children.Add(baseCanvasCover);

        }
        #endregion

        #region Region: Next cell stuff

        // This runs the Game of Life
        static void SetNextGrid()
        {

            #region Region : Variables
            int stable = 0;
            stop = left = right = 0;
            int stapend = 6;
            int prevstap = stap;
            int Highest_y = 0, Highest_x = 0;

            // An array of notes in teh C major scale
            Cmajor = new Pitch[49] {Pitch.C2, Pitch.D2, Pitch.E2, Pitch.F2, Pitch.G2, Pitch.A2, Pitch.B2,
                                      Pitch.C3, Pitch.D3, Pitch.E3, Pitch.F3, Pitch.G3, Pitch.A3, Pitch.B3,
                                        Pitch.C4, Pitch.D4, Pitch.E4, Pitch.F4, Pitch.G4, Pitch.A4, Pitch.G4,
                                          Pitch.C5, Pitch.D5, Pitch.E5, Pitch.F5, Pitch.G5, Pitch.A5, Pitch.G5,
                                            Pitch.C6, Pitch.D6, Pitch.E6, Pitch.F6, Pitch.G6, Pitch.A6, Pitch.B6,
                                              Pitch.C7, Pitch.D7, Pitch.E7, Pitch.F7, Pitch.G7, Pitch.A7, Pitch.B7,
                                                Pitch.C8, Pitch.D8, Pitch.E8, Pitch.F8, Pitch.G8, Pitch.A8, Pitch.B8 };

            // An array of notes n the A minor Scale
            Aminor = new Pitch[49] {Pitch.A2, Pitch.B2,
                                      Pitch.C3, Pitch.D3, Pitch.E3, Pitch.F3, Pitch.G3, Pitch.A3, Pitch.B3,
                                        Pitch.C4, Pitch.D4, Pitch.E4, Pitch.F4, Pitch.G4, Pitch.A4, Pitch.G4,
                                          Pitch.C5, Pitch.D5, Pitch.E5, Pitch.F5, Pitch.G5, Pitch.A5, Pitch.G5,
                                            Pitch.C6, Pitch.D6, Pitch.E6, Pitch.F6, Pitch.G6, Pitch.A6, Pitch.B6,
                                              Pitch.C7, Pitch.D7, Pitch.E7, Pitch.F7, Pitch.G7, Pitch.A7, Pitch.B7,
                                                Pitch.C8, Pitch.D8, Pitch.E8, Pitch.F8, Pitch.G8, Pitch.A8, Pitch.B8,
                                                 Pitch.C9, Pitch.D9, Pitch.E9, Pitch.F9, Pitch.G9 };
            blank = 0;


            //Set  if stable stop variable back to zero after 10 cycles of the loop
            if (stap >= stapend + 1) stap = 0;
            #endregion

            for (int loopy = 0; loopy < SquaresDown; loopy++)
            {
                for (int lopy = 0; lopy < SquaresAcross; lopy++)
                {
                    // Sets all the generations back one
                    theCellsgen3[loopy, lopy].status = theCellsgen2[loopy, lopy].status;
                    theCellsgen2[loopy, lopy].status = theCells[loopy, lopy].status;
                }
            }

            #region Region : Check Cells Loop
            for (int loop = 1; loop < SquaresDown - 1; loop++)
            {
                for (int lop = 1; lop < SquaresAcross - 1; lop++)
                {

                    int neighboursAlive = 0;

                    // Checks each surrounding cells of the current one and counts
                    // the amount living cells around it
                    if (theCellsgen2[loop - 1, lop - 1].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop - 1, lop].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop - 1, lop + 1].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop, lop - 1].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop, lop + 1].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop + 1, lop - 1].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop + 1, lop].status != 0) neighboursAlive++;
                    if (theCellsgen2[loop + 1, lop + 1].status != 0) neighboursAlive++;

                    // If the Cell was not created by the Kinect so this
                    if (theCellsgen2[loop, lop].body != 1)
                    {
                        // Then the rules of life: if there are more than three neighbours, this cell dies:
                        if (neighboursAlive > 3) { theCells[loop, lop].status = 0; }

                        // If there are exactly two or three neighbours, this cell maintains its state:
                        else if ((neighboursAlive == 3 || neighboursAlive == 2) & theCellsgen2[loop, lop].status == 1)
                        {theCells[loop, lop].status = 1; }

                        // If there are exactly three neighbours, this cell comes to life:
                        else if (neighboursAlive == 3 & theCellsgen2[loop, lop].status == 0)
                        {
                            theCells[loop, lop].status = 1;
                        }
                        // If there are less than two neighbours, this cell dies:
                        else { theCells[loop, lop].status = 0; }

                        // increase right or left overall values
                        if (lop < (SquaresAcross - 2) / 2 + 1 && theCells[loop, lop].status == 1) { left++; theCells[loop, lop].rect.Fill = new SolidColorBrush(LeftColour); }
                        else if (lop > (SquaresAcross - 2) / 2 && theCells[loop, lop].status == 1) { right++; theCells[loop, lop].rect.Fill = new SolidColorBrush(RightColour); }
                        else theCells[loop, lop].rect.Fill = new SolidColorBrush(Colors.Transparent);

                        theCellsgen2[loop, lop].body = 0;
                    }

                    // If the current cell is 1 then draw a cell
                    if (lop < (SquaresAcross - 2) / 2 + 1 && theCellsgen2[loop, lop].status == 1) theCellsgen2[loop, lop].rect.Fill = new SolidColorBrush(LeftColour);
                    else if (lop > (SquaresAcross - 2) / 2 && theCellsgen2[loop, lop].status == 1) { theCellsgen2[loop, lop].rect.Fill = new SolidColorBrush(RightColour); }
                    else theCellsgen2[loop, lop].rect.Fill = new SolidColorBrush(Colors.Transparent);

                    // If the current cell is 1 then draw a cell
                    if (lop < (SquaresAcross - 2) / 2 + 1 && theCellsgen3[loop, lop].status == 1) theCellsgen3[loop, lop].rect.Fill = new SolidColorBrush(LeftColour);
                    else if (lop > (SquaresAcross - 2) / 2 && theCellsgen3[loop, lop].status == 1) { theCellsgen3[loop, lop].rect.Fill = new SolidColorBrush(RightColour); }
                    else theCellsgen3[loop, lop].rect.Fill = new SolidColorBrush(Colors.Transparent);
                        
                    // Check that there any cells alive in gen2
                    if (theCellsgen2[loop, lop].status == 1) { blank2++; }

                    // Check that there any cells alive in gen2
                    if (theCellsgen3[loop, lop].status == 1) { stop++; }

                    // Check if the current pattern displayd is stable
                    if (theCellsgen3[loop, lop].status == 1 && theCellsgen2[loop, lop].status == 1) { stable++; }

                    // Finding values for the highest y value
                    if (theCells[loop, lop].status == 1 && loop >= Highest_y)
                        Highest_y = theCells[loop, lop].status * loop;

                    // Finding values for the highest x value
                    if (theCells[loop, lop].status == 1 && lop >= Highest_x)
                        Highest_x = theCells[loop, lop].status * lop;

                   


                }


            }

            // If all the squares that have stayed alive are 
            // equal to the amount of squares in the grid increase stap
            if (stop == stable) { stap++; }
            // increase prevgenstop whenever there are no cells alive
            if (stop == 0) { prevgenstop++; }
            if (stap >= stapend) { prevgentab++; }

            // Records teh right value and sets it back one generation
            bottomgen4 = bottomgen3;
            bottomgen3 = bottomgen2;
            bottomgen2 = right;

            #endregion

            #region Region : MIDI
            if (stap == prevstap) stap = 0;
            if (stap == prevstap || stap < 2)
            {
                // Starts teh MIDI_Notes method
                MIDI_Notes(Highest_y, Highest_x, left, right, stable, stop);
            }
            #endregion


            // If all the cells have been dead (stop == 0) for 3 generations, to allow for gen2 and gen3
            // or if the game has been stable for 6 generations 
            if (((stop == 0) && (prevgenstop == 3)) || ((stap >= stapend) && prevgentab == 3))
            {
                prevgentab = 0;

                // If al cells are dead
                if (stop == 0 && prevgenstop == 3)
                {
                    stap = 0; // if no squares exist stop or if stap is at 10 stop
                    prevgenstop = 0;

                    for (int loopy = 0; loopy < SquaresDown; loopy++)
                    {
                        for (int lopy = 0; lopy < SquaresAcross; lopy++)
                        {
                            //sets both arrays to zero as not to interfere with new generations
                            theCellsgen3[loopy, lopy].status = 0;
                            theCellsgen2[loopy, lopy].status = 0;
                            theCells[loopy, lopy].status = 0;
                        }
                    }

                }
                //turn off all notes
                outputDevice.SilenceAllNotes();

                // If teh game is not looping stop it
                if (LoopGame != 1)
                {
                    timer.Stop();
                    timer.Enabled = false;
                }

                // Turn off all notes, SilenceALlNotes doesn't always seem to work
                outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);
            }
        }

        // This method allows the SetNextGrid method to run without
        // needing any parameters
        static void GoL(object sender, EventArgs f)
        {
            SetNextGrid();
        }


        #endregion

        #region Region : MIDI Notes
        static void MIDI_Notes(int Highest_y, int Highest_x, int top, int bottom, int stable, int stop)
        {
            double scaleSize = 49;
            int velocity = 100;

            // Scale the highest y and x values by the potential highest value
            double Highest_y_scale = (double)Highest_y / (double)(SquaresDown - 1);
            double Highest_x_scale = (double)Highest_x / (double)(SquaresAcross - 1);
            double increase = (stop / 64.0) * scaleSize;
            //amount of secondsthe pad chord is held for
            double hold = 7;
            // Random number generators
            Random random = new Random();
            int randomOff = random.Next(3, 6);
            int threshCheck = 0;
            int lower = 7;
            // increase the note one or two octaves
            top += 7;
            //bottom += 7;

            // wraps the values if they exceed the maximum
            if (bottom >= scaleSize - 4) { bottom = (int)scaleSize - 4; }
            if (top >= scaleSize - 4) { top = (int)scaleSize - 4; }

            if (top > guitPlay)
            {
                // MIDI Note Off
                outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                // MIDI Note On
                outputDevice.SendNoteOn(Channel.Channel1, Aminor[top], velocity);

                topprev = top;

                // If the last note had a high likeliness of playing half the likeliness
                if (guitPlay == 7) guitPlay = 14;
                else if (guitPlay == 14) guitPlay = 21;
            }
            // If the note is stopped but not played again make it likely it will play next time
            else { outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0); guitPlay = 7; }


            //Averages out the values from the past 3 generations
            bottomav = ((double)bottom + (double)bottomgen2 + (double)bottomgen3 + (double)bottomgen4) / 4.0;

            // if the average of the bottom values is less than the amount of
            // time the chords should be held progress towards an off state
            if (bottomav <= hold) { off++; padThresh--; }
            else off = 0;

            // when off reaches 4 stop the chord pad
            if (off >= randomOff)
            {
                // MIDI Chord Off
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);

                off = 0;

                threshCheck = 0;
            }
            // Chords are either played when the bottom values add to 9 or greater
            // or if it is the first (or 0th) generation of this game
            else if (bottom - lower > padThresh || first < 1)
            {

                // MIDI Chord Off
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);

                // MIDI Chord On
                outputDevice.SendNoteOn(Channel.Channel2, Cmajor[bottom], velocity);
                outputDevice.SendNoteOn(Channel.Channel2, Cmajor[bottom + 2], velocity);
                outputDevice.SendNoteOn(Channel.Channel2, Cmajor[bottom + 4], velocity);

                bottomprev = bottom;

                threshCheck = 1;
            }
            else if (stable == stop) { threshCheck = 0; }



            // If a chord has not been played for a while make it more likely one will
            if (threshCheck == 1) padThresh = 9;
            else if (threshCheck == 0) padThresh = 3;

            // increase the first value, if this is greater than 0 then it 
            // is not the first generation of this game
            first = 1;

            firstPlay++;

        }
        #endregion

        #region Region : Button Stuff

        // This method gets a list of all the MIDI outputs
        // adapted from Dave Pearce's Life_Music
        static List<OutputDevice> GetDeviceList()
        {
            // Sets up a list for outputs
            devices = new List<OutputDevice>();
            used = new List<int>();

            // Counts the amount of installed devices
            int howMany = OutputDevice.InstalledDevices.Count;
           
            //Adds each output device to the list
            foreach (OutputDevice dev in OutputDevice.InstalledDevices)
            {
                devices.Add(dev);
                used.Add(0);

            }
            return devices;
        }

        // This method checks whether the MIDI output should be changed
        private void cmbMIDIDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            // Then select the new one and open it if possible:
            if ((sender as System.Windows.Controls.ComboBox).SelectedIndex < Midi.OutputDevice.InstalledDevices.Count)
            {

                if (sender == Lead_Channel)
                {
                    // If the game has gone through one or more gnerations stop all notes being played
                    if (firstPlay > 0)
                    {
                        outputDevice.SilenceAllNotes();

                        outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                        outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                        outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                        outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);
                    }

                    //close the current output device
                    if (outputDevice.IsOpen == true) outputDevice.Close();

                    Device = (sender as System.Windows.Controls.ComboBox).SelectedIndex;

                    // Set the new output device
                    outputDevice = OutputDevice.InstalledDevices[Device];

                    // Open the output device
                    if (outputDevice.IsOpen == false) outputDevice.Open();

                    // Set up the new instruments for this device
                    outputDevice.SendProgramChange(Channel.Channel1, Instrument.ElectricGuitarJazz);
                    outputDevice.SendProgramChange(Channel.Channel2, Instrument.Pad2Warm);


                }

                // Set the initial Timbre values
                outputDevice.SendControlChange(Channel.Channel15, Midi.Control.Volume, 63);
                outputDevice.SendControlChange(Channel.Channel16, Midi.Control.Volume, 95);

            }


        }

        // This method runs when Start is clicked
        private void Start_clicked(object sender, RoutedEventArgs e)
        {
            // If Loop is checked this button does nothing
            if (LoopGame != 1)
            {
                // Sets first generation of the game and then starts the timer
                // to count down to the next one
                SetNextGrid();
                timer.Interval = UpdateSpeed;
                timer.Enabled = true;
                timer.Start();
            }

        }

        // This runs when Stop is clicked
        private void Stop_clicked(object sender, RoutedEventArgs e)
        {
            // If Loop is checke this button does nothing
            if (LoopGame != 1)
            {
                // If the game has gone through one or more generations stop all notes
                if (firstPlay > 0)
                {
                    outputDevice.SilenceAllNotes();

                    outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                    outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                    outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                    outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);
                }

                timer.Stop();
                timer.Enabled = false;
            }
        }

        // This method runs when clear is clicked
        private void Clear_clicked(object sender, RoutedEventArgs e)
        {
            // If the game has gone through one or more gnerations stop all notes
            if (firstPlay > 0)
            {
                outputDevice.SilenceAllNotes();

                outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);
            }

            // If loop is not checked stop the timer
            if (LoopGame != 1)
            {
                timer.Stop();
                timer.Enabled = false;
            }

            // Set all the cells to dead
            for (int loopy = 0; loopy < SquaresDown; loopy++)
            {
                for (int lopy = 0; lopy < SquaresAcross; lopy++)
                {
                    //sets all the cells back to 0
                    theCells[loopy, lopy].status = 0;
                    theCellsgen2[loopy, lopy].status = 0;
                    theCellsgen3[loopy, lopy].status = 0;

                    if (loopy < SquaresDown - 2 && lopy < SquaresAcross - 2)
                    {
                        theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent); ;
                        theCellsgen2[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent);
                        theCellsgen3[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent);
                    }


                }
            }
           
        }

        // This method is run when reandom is clicked
        private void Random_clicked(object sender, RoutedEventArgs e)
        {
            Random random = new Random();


            for (int loopy = 0; loopy < SquaresDown - 2; loopy++)
            {
                for (int lopy = 0; lopy < SquaresAcross - 2; lopy++)
                {
                    // Sets a random number between 0 and 100
                    int randomNumber = random.Next(0, 100);

                    // Gives a 1 in 3 chance that a cell will become alive
                    if (randomNumber > 66)
                    {
                        theCells[loopy + 1, lopy + 1].status = 1;
                        if (lopy < (SquaresAcross - 2) / 2 + 1 && theCells[loopy + 1, lopy + 1].status == 1) { left++; theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(LeftColour); }
                        else if (lopy > (SquaresAcross - 2) / 2 && theCells[loopy + 1, lopy + 1].status == 1) { right++; theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(RightColour); }
                    }
                    // If the random number is not above 66 then it is dead.
                    else
                    {
                        theCells[loopy + 1, lopy + 1].status = 0;
                        theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent);
                    }
                }
            }

            //Draw_rectangles_grid(baseCanvas, secondCanvas, thirdCanvas);

        }

        // This method is run when the value of the Kinect angle is changed
        private void Angle_Changed(object sender, MouseButtonEventArgs e)
        {
            // Code adapted from example code provided with the Kinect SDK
            int angleToSet = (int)AngleSlider.Value;
            bool backgroundUpdateInProgress = true;
            System.Threading.Tasks.Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            // Check for not null and running
                            if ((this._KinectDevice != null) && this._KinectDevice.IsRunning)
                            {


                                // We must wait at least 1 second, and call no more frequently than 15 times every 20 seconds
                                // So, we wait at least 1350ms afterwards before we set backgroundUpdateInProgress to false.
                                this._KinectDevice.ElevationAngle = angleToSet;
                                Thread.Sleep(1350);

                            }
                        }
                        finally
                        {
                            backgroundUpdateInProgress = false;
                        }
                    }).ContinueWith(results =>
                    {
                        // This can happen if the Kinect transitions from Running to not running
                        // after the check above but before setting the ElevationAngle.
                        if (results.IsFaulted)
                        {
                            var exception = results.Exception;

                        }
                    });

        }

        // This method is run when the movement is checked
        private void Movement_Checked(object sender, RoutedEventArgs e)
        {

            Movement = 1;

        }

        // This method is run when Movement is unchecked
        private void Movement_Unchecked(object sender, RoutedEventArgs e)
        {

            Movement = 0;

        }

        // This method is run when Loop is checked
        private void Loop_Checked(object sender, RoutedEventArgs e)
        {
            //starts the game looping
            LoopGame = 1;

            //sets the boarders of Start and Pause to red
            Start.BorderBrush = new SolidColorBrush(Colors.Red);
            Pause.BorderBrush = new SolidColorBrush(Colors.Red);

            //if it's not already enable timer
            if (timer.Enabled == false)
            {
                SetNextGrid();
                timer.Interval = UpdateSpeed;
                timer.Enabled = true;
                timer.Start();
            }
        }

        // This method is run when loop is unchecked
        private void Loop_Unchecked(object sender, RoutedEventArgs e)
        {
            //stops the code looping
            LoopGame = 0;

            //converts the hexidecimal value given in the xaml MAindWindow code to a Color
            Color original = (Color)ColorConverter.ConvertFromString("#FF707070");

            //set pause and start borders back to their original colour
            Start.BorderBrush = new SolidColorBrush(original);
            Pause.BorderBrush = new SolidColorBrush(original);

            // If all the generations are blank then stop the timer, if not let it finish naturally
            if (blank == 0 && blank2 == 0 && stop == 0)
            {
                timer.Stop();
                timer.Enabled = false;

            }
        }

        // This method is run when Reset is clicked
        private void Reset_clicked(object sender, RoutedEventArgs e)
        {
            // Resets all cliders and buttons to their initial values

            Lead_Channel.SelectedIndex = 0;

            UpdateSpeed = 1000;

            Update.Value = UpdateSpeed;

            timer.Interval = UpdateSpeed;

            LoopBox.IsChecked = false;

            MovementSquares.IsChecked = false;

            LoopGame = 0;

            Movement = 0;

            MoveColour = Colors.Purple;

            // Stops all notes being played
            if (firstPlay > 0)
            {
                outputDevice.SilenceAllNotes();

                outputDevice.SendNoteOff(Channel.Channel1, Aminor[topprev], 0);

                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 2], 0);
                outputDevice.SendNoteOff(Channel.Channel2, Cmajor[bottomprev + 4], 0);
            }

            // Stops timer
            
                timer.Stop();
                timer.Enabled = false;

            // Sets all teh cells to dead and transparent
            for (int loopy = 0; loopy < SquaresDown; loopy++)
            {
                for (int lopy = 0; lopy < SquaresAcross; lopy++)
                {
                    //sets all the quares back to 0
                    theCells[loopy, lopy].status = 0;
                    theCellsgen2[loopy, lopy].status = 0;
                    theCellsgen3[loopy, lopy].status = 0;

                    if (loopy < SquaresDown - 2 && lopy < SquaresAcross - 2)
                    {
                        theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent); ;
                        theCellsgen2[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent);
                        theCellsgen3[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent);
                    }


                }
            }

            // sets the Kinect angle to 0
            int angleToSet = 0;
            bool backgroundUpdateInProgress = true;
            System.Threading.Tasks.Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            // Check for not null and running
                            if ((this._KinectDevice != null) && this._KinectDevice.IsRunning)
                            {


                                // We must wait at least 1 second, and call no more frequently than 15 times every 20 seconds
                                // So, we wait at least 1350ms afterwards before we set backgroundUpdateInProgress to false.
                                this._KinectDevice.ElevationAngle = angleToSet;
                                Thread.Sleep(1350);

                            }
                        }
                        finally
                        {
                            backgroundUpdateInProgress = false;
                        }
                    }).ContinueWith(results =>
                    {
                        // This can happen if the Kinect transitions from Running to not running
                        // after the check above but before setting the ElevationAngle.
                        if (results.IsFaulted)
                        {
                            var exception = results.Exception;

                        }
                    });
            // Sets the slider controlling the Kinect angle to 0
            AngleSlider.Value = angleToSet;

        }

        // This method is run when the speed slider changes value
        private void Speed_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Updates the speed value
            UpdateSpeed = (int)Update.Value;

            // Tells the timer that a change to the speed has been made
            timer.Interval = UpdateSpeed;
        }



        #endregion


        #region Methods

        // Adapted from example code taken from Kinect Programming with the Microsoft Kinect SDK
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }


        int globalY, globalX, globalZ;

        // This method is adapted from Kinect Programming with the Microsoft Kinect SDK
        private void KinectDevice_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    frame.CopyPixelDataTo(this._DepthPixelData);
                    CreateBetterShadesOfGray(frame, this._DepthPixelData);
                    //CreateDepthHistogram(frame, this._DepthPixelData);
                }
            }
            string pan = "L";

            // If pan is less that 45% it is redefined as between 0 and 100% on the left
            if (globalX < 45){ pan = "L"; globalX = (50 - globalX) * 2;}
            // If pan is greater than 55% it is redefined as betwen 0 and 100% on the right
            else if (globalX > 55) { pan = "R"; globalX = (globalX - 50) * 2; }
            // Else it is in the center
            else { pan = "C"; globalX = 0; }

            // Displays the values of pan volume effect speed and angle
            FramesPerSecondElement.Text = "Pan = " + pan + globalX + "%\nEffect = " + (100 - globalY) + "%\nVolume = " + globalZ + "%\nElevation:" + AngleSlider.Value + "\nSpeed (s):" + (double)UpdateSpeed / 1000.0;
        }

        // This method sorts and maipulates the incoming depth data
        // it is also adapted from Kinect Programming with the Microsoft Kinect SDK 
        private void CreateBetterShadesOfGray(DepthImageFrame depthFrame, short[] pixelData)
        {
            // Initialise the variables
            int depth;
            int colourR;
            int colourG;
            int colourB;
            int bytesPerPixel = 4;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel];
            int test = pixelData.Length;
            int highestDepth = 0;
            int highestDepthpos = 0;
            int highestDepthx = 0;
            int highestDepthy = 0;
            double panControl = 0;
            double reverbControl = 0;

            // Multiplies the width of the depth image divides it by the amount of cells on teh x axis
            // Does the same for the height and y axis then multiplies them finding the amount of pixels 
            // under each cell
            int pixelsPerSquare = (depthFrame.Width / (SquaresAcross - 2)) * (depthFrame.Height / (SquaresDown - 2));

            // If cells are being added by the kinect reset them to zero before the loop starts
            if (Movement == 1)
            {
                for (int loopy = 0; loopy < SquaresDown; loopy++)
                {
                    for (int lopy = 0; lopy < SquaresAcross; lopy++)
                    {
                        theCells[loopy, lopy].pixel = 0;

                    }
                }
            }

            // This loop goes through every pixel and applies the depth stream data to it
            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel)
            {
                // Takes the current pixel
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // if it is the first loop the closest point is the same as the end of the usable area
                // and its position is 0,0
                if (j == 0) { highestDepth = (int)(HighThreshold); highestDepthpos = i; }

                // if the current pixel is closer in depth than the current value of the closest point
                // and is within the set usable area then make this the new closest point
                // and find its x, y coordinates
                if (highestDepth > depth && depth > LowThreshold && depth <= HighThreshold)
                { highestDepth = depth; highestDepthpos = i; highestDepthy = i / depthFrame.Width; highestDepthx = i - highestDepthy * depthFrame.Width; }

                // find the x, y coordinates of the current pixel
                int y = i / depthFrame.Width;
                int x = i - y * depthFrame.Width;

                // find the x, y coordinates in relation to the game of "Life"
                int gridx = (int)Math.Ceiling((double)x / (double)(depthFrame.Width) * (SquaresAcross - 2));
                int gridy = (int)Math.Ceiling(((double)y / (double)depthFrame.Height) * (SquaresDown - 2));

                


                // If the depth is closer that LowThreshold point it is white 
                // and not within the usable area
                if (depth < LowThreshold)
                {
                    colourB = 255;
                    colourG = 255;
                    colourR = 255;

                }

                // If the depth is further that HighThreshold point it is white 
                // and not within the usable area
                else if (depth > HighThreshold)
                {
                    colourB = 255;
                    colourG = 255;
                    colourR = 255;
                }

                // if the value is inbetween LowThreshold and HighThreshold it is pink
                // and within the usable area
                else
                {
                    // The RGB values are decided by the x,y,z coordinates of the closest point
                    colourB = (int)Coly;
                    colourG = (int)Colx;
                    colourR = (int)Colz;
                    
                    // If cells are being created by teh Kinect increase the pixels value
                    // within the relative cLifeCell class
                    if (Movement == 1) theCells[gridy, gridx].pixel++;
                }

                // Set the RGB values
                enhPixelData[j] = (byte)colourB;
                enhPixelData[j + 1] = (byte)colourG;
                enhPixelData[j + 2] = (byte)colourR;
            }

           

            // If cells are being created by the kinect
            if (Movement == 1)
            {
                for (int loopy = 0; loopy < SquaresDown - 2; loopy++)
                {
                    for (int lopy = 0; lopy < SquaresAcross - 2; lopy++)
                    {
                        // If the pixels value of the current cell is greater than half the potential pixels behind a cell
                        // and a cell here has not been created by the game itself  or if timer is off create one
                        if (theCells[loopy + 1, lopy + 1].pixel > pixelsPerSquare / 2 && (theCells[loopy + 1, lopy + 1].status != 1 || timer.Enabled == false))
                        {
                            // The cells are then the inverted colour of the Depth display 
                            MoveColour.B = (byte)(255 - Coly);
                            MoveColour.G = (byte)(255 - Colx);
                            MoveColour.R = (byte)(255 - Colz);
                            // Set the colour the status and teh body to one and make it somewhat opaque
                            theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(MoveColour);
                            theCells[loopy + 1, lopy + 1].status = 1;
                            // body lets the game know ehether it was created by the game or the Kinect
                            theCells[loopy + 1, lopy + 1].body = 1;
                            theCells[loopy + 1, lopy + 1].rect.Opacity = 0.85;
                        }
                        // Else make it transparent, set everything to zero
                        else if (theCells[loopy + 1, lopy + 1].pixel <= pixelsPerSquare / 2
                                    && theCells[loopy + 1, lopy + 1].body == 1)
                        {
                            theCells[loopy + 1, lopy + 1].rect.Fill = new SolidColorBrush(Colors.Transparent);
                            theCells[loopy + 1, lopy + 1].status = 0;
                            theCells[loopy + 1, lopy + 1].body = 0;

                        }
                    }
                }
            }
             // Turns the x,y,z values into percentages
            double xPerc = (double)highestDepthx / (double)(depthFrame.Width);
            double yPerc = ((double)highestDepthy / (double)depthFrame.Height);
            double zPerc = ((double)highestDepth - (double)LowThreshold) / ((double)HighThreshold - (double)LowThreshold);

            // Multiply those percentages by 255, giving a colour value
            Colx = xPerc * 255;
            Coly = yPerc * 255;
            Colz = zPerc * 255;

            // makes a new number 
            double newVal = highestDepth;
            double newValpos = highestDepthpos;
            double newValx = Colx;
            double newValy = Coly;
            double newValz = Colz;

            //sets the current position of the array to equal to this new random number
            lastfewX[_xfilterindex] = newVal;
            lastfewXpos[_xfilterindex] = newValpos;
            lastfewXx[_xfilterindex] = newValx;
            lastfewXy[_xfilterindex] = newValy;
            lastfewXz[_xfilterindex] = newValz;

            // if the current array position is equal to 4 then it equals 0
            // if not then it equals the current position + 1
            _xfilterindex = _xfilterindex == FILTERARRAYLENGTH - 1 ? 0 : _xfilterindex + 1;

            if (run > FILTERARRAYLENGTH - 1)
            {
                // makes a clone (or copy) of the current array
                sortingArray = (double[])lastfewX.Clone();

                // makes a clone (or copy) of the current array
                sortingArraypos = (double[])lastfewXpos.Clone();

                sortingArrayx = (double[])lastfewXx.Clone();

                sortingArrayy = (double[])lastfewXy.Clone();

                sortingArrayz = (double[])lastfewXz.Clone();


                // dunno
                Array.Sort(sortingArray);

                // dunno
                Array.Sort(sortingArraypos);
                Array.Sort(sortingArrayx);
                Array.Sort(sortingArrayy);
                Array.Sort(sortingArrayz);


                // sets the new value to be at the 2nd position of the array
                highestDepth = (int)Math.Floor(((sortingArray[1] + sortingArray[2] + sortingArray[3]) / 3));
                Colx = ((sortingArrayx[1] + sortingArrayx[2] + sortingArrayx[3]) / 3);
                Coly = ((sortingArrayy[1] + sortingArrayy[2] + sortingArrayy[3]) / 3);
                Colz = ((sortingArrayz[1] + sortingArrayz[2] + sortingArrayz[3]) / 3);

            }
            else
            {
                run++;
            }




            this._DepthImage.WritePixels(this._DepthImageRect, enhPixelData, this._DepthImageStride, 0);

            // Creates a value between 1 and zero that will then later be
            // mulitplied by the highest value needed when manipulating the timbre
            Timbre = (((LoDepthThreshold * 1.5) - highestDepth) / ((LoDepthThreshold * 1.35) - (LoDepthThreshold / 1.8)));

            int ytest = highestDepthpos / depthFrame.Width;
            int xtest = highestDepthpos - ytest * depthFrame.Width;

            // Bodge:


            panControl = (int)Math.Floor((double)highestDepthx / (double)(depthFrame.Width) * 127.0);

            reverbControl = (int)Math.Floor(((double)highestDepthy / (double)depthFrame.Height) * 127.0);

            

            

            // Multiplies the percenage by 127
            int TimbreCheck = (int)(Timbre * 127);

            // This caps the value at 127
            if (TimbreCheck >= 127) TimbreCheck = 127;
            else if (TimbreCheck <= 0) TimbreCheck = 0;

            if (panControl >= 127) panControl = 127;
            else if (panControl <= 0) panControl = 0;

            if (reverbControl >= 127) reverbControl = 127;
            else if (reverbControl <= 0) reverbControl = 0;

            // if the people are out of the usable area (in front or behind) then all values are set to the middle
            if (highestDepth >= LoDepthThreshold * 1.5 || highestDepth <= LoDepthThreshold / 2)
            {
                TimbreCheck = 63;
                panControl = 63;
                reverbControl = 63;
                Timbre = 1;
            }

            globalY = (int)(reverbControl / 127 * 100); globalX = (int)(panControl / 127 * 100); globalZ = (int)(Timbre * 100);

            // Makes the Timbre controlled by the Kinect value

            // z plane used to increase dry level of reverb and overall mix of chorus
            outputDevice.SendControlChange(Channel.Channel1, Midi.Control.Volume, (int)(TimbreCheck * 0.75));

            // z plane used to incrase wet level of reverb
            outputDevice.SendControlChange(Channel.Channel2, Midi.Control.Volume, (int)(TimbreCheck * 0.5));

            // x value used to control panning panning plugin
            outputDevice.SendControlChange(Channel.Channel1, Midi.Control.Pan, (int)panControl);
            outputDevice.SendControlChange(Channel.Channel2, Midi.Control.Pan, (int)panControl);

            // y value used to control the master volume of both midi plugins
            outputDevice.SendControlChange(Channel.Channel1, Midi.Control.ReverbLevel, (int)reverbControl);

            // y value used to control the master volume of both midi plugins
            outputDevice.SendControlChange(Channel.Channel2, Midi.Control.ReverbLevel, 127 - (int)reverbControl);

            // Stores the last highestDepth value
            highestCheck = highestDepth;
        }







        #region Reagion : Audio Histogram
        private void CreateDepthHistogram(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth;
            int[] depths = new int[4096];
            double chartBarWidth = Math.Max(3, DepthHistogram.ActualWidth / depths.Length);
            int maxValue = 0;


            DepthHistogram.Children.Clear();


            //First pass - Count the depths.
            for (int i = 0; i < pixelData.Length; i++)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth >= LoDepthThreshold && depth <= HiDepthThreshold)
                {
                    depths[depth]++;
                }
            }


            //Second pass - Find the max depth count to scale the histogram to the space available.
            //              This is only to make the UI look nice.
            for (int i = 0; i < depths.Length; i++)
            {
                maxValue = Math.Max(maxValue, depths[i]);
            }


            //Third pass - Build the histogram.
            for (int i = 0; i < depths.Length; i++)
            {
                if (depths[i] > 0)
                {
                    Rectangle r = new Rectangle();
                    r.Fill = Brushes.Black;
                    r.Width = chartBarWidth;
                    r.Height = DepthHistogram.ActualHeight * (depths[i] / (double)maxValue);
                    r.Margin = new Thickness(1, 0, 1, 0);
                    r.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    DepthHistogram.Children.Add(r);
                }
            }
        }
        #endregion


        #endregion Methods



        #region Properties
        public KinectSensor KinectDevice
        {
            get { return this._KinectDevice; }
            set
            {
                if (this._KinectDevice != value)
                {
                    //Uninitialize
                    if (this._KinectDevice != null)
                    {
                        this._KinectDevice.Stop();
                        this._KinectDevice.DepthFrameReady -= KinectDevice_DepthFrameReady;
                        this._KinectDevice.DepthStream.Disable();

                        this.DepthImage.Source = null;
                        this._DepthImage = null;
                    }

                    this._KinectDevice = value;

                    //Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            this._KinectDevice.DepthStream.Enable();

                            DepthImageStream depthStream = this._KinectDevice.DepthStream;
                            this._DepthImage = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                            this._DepthImageRect = new Int32Rect(0, 0, (int)Math.Ceiling(this._DepthImage.Width), (int)Math.Ceiling(this._DepthImage.Height));

                            this._DepthImageStride = depthStream.FrameWidth * 4;
                            this._DepthPixelData = new short[depthStream.FramePixelDataLength];
                            this.DepthImage.Source = this._DepthImage;
                            this.DepthImage.Height = mainCanvas.Height;
                            this.DepthImage.Width = mainCanvas.Width;
                            this._KinectDevice.DepthFrameReady += KinectDevice_DepthFrameReady;
                            this._KinectDevice.Start();
                            //this.DepthImage.Stretch = Stretch.Fill;
                            this.DepthImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            this.DepthImage.VerticalAlignment = VerticalAlignment.Top;
                            //mainCanvas.Children.Add(DepthImage);

                            this._StartFrameTime = DateTime.Now;

                            elevang = (double)_KinectDevice.ElevationAngle;
                        }
                    }
                }
            }
        }
        #endregion Properties


    }
}
