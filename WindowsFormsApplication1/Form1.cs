using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        //SECRET SETTINGS:
        //Snake length at startup
        int nInitialSnakeLength = 4;
        //Maximum number of turds.
        int nMaxTurds = 20;
        //Use Image Resources?
        bool bUseImages = true;
        //Snake Speed in milliseconds. Lower values = faster.
        int nSnakeSpeed = 66;
        //Setting for whether sickness inverts controls or speeds up.
        bool bSickInverts = false;

        //Global Declarations & States
        PictureBox[] grid = new PictureBox[1024];
        CheckBox checkWarpWalls = new CheckBox();
        CheckBox checkTurds = new CheckBox();
        Label labelScore = new Label();
        Label labelHiScore = new Label();
        static Random _r = new Random();
        private System.Timers.Timer _timer1 = new System.Timers.Timer();
        bool bRunning = false;
        bool bWarpWalls;
        bool bGrow = false;
        bool bSick = false;
        int nPoints;
        //Direction of movement...
        //Up = 1; Right = 2; Down = 3; Left = 4
        int nDir = 2;
        List<int> snake = new List<int>();
        List<int> snakedirs = new List<int>();
        List<int> turds = new List<int>();
        delegate void SetScoreCallback();
        delegate void SetHiScoreCallback();

        public Form1()
        {
            InitializeComponent();

            //Set up timer
            _timer1.Interval = nSnakeSpeed;
            _timer1.Elapsed += OnTimerElapsed;
            _timer1.AutoReset = false;

            //Create "Warp Walls" checkbox.
            //This toggles the behavior of hitting edges 
            //causing game over.
            checkWarpWalls = new CheckBox();
            checkWarpWalls.Location = new Point(16, 529);
            checkWarpWalls.Text = "Warp Walls";
            checkWarpWalls.Checked = true;
            this.Controls.Add(checkWarpWalls);
            checkWarpWalls.Focus();

            //Create "Turds" checkbox.
            //This toggles the behavior of pooping.
            //Eating poops makes you sick.
            //Eating a lot of poops causes game over.
            checkTurds = new CheckBox();
            checkTurds.Location = new Point(480, 529);
            checkTurds.Text = "Turds";
            checkTurds.Checked = true;
            this.Controls.Add(checkTurds);

            //Create score label
            labelScore = new Label();
            labelScore.Location = new Point(470, 2);
            labelScore.ForeColor = Color.PaleGreen;
            labelScore.Font = new Font(labelScore.Font, FontStyle.Bold);
            labelScore.Height = 14;
            this.Controls.Add(labelScore);

            //Create high score label
            labelHiScore = new Label();
            labelHiScore.Location = new Point(16, 2);
            labelHiScore.ForeColor = Color.Black;
            labelHiScore.Height = 14;
            this.Controls.Add(labelHiScore);

            this.BackColor = Color.DimGray;
            this.KeyPreview = true;

            //Declare key handler(s)
            this.checkWarpWalls.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(checkBox1_PreviewKeyDown);
            this.checkTurds.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(checkBox1_PreviewKeyDown);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(checkBox1_PreviewKeyDown);

            //Create board: a 32x32 grid of 16px square pictureboxes.
            int xpos = 16;
            int ypos = 16;

            for (int i = 0; i < 1024; i++)
            {
                grid[i] = new PictureBox();
                grid[i].Location = new Point(xpos, ypos);
                grid[i].Size = new Size(16, 16);
                grid[i].Visible = true;
                this.Controls.Add(grid[i]);

                //if you're done with a row (iow, creating a 33rd box per row), 
                //reset x, and increment y to start the next row.
                if ((i + 1) % 32 == 0)
                {
                    xpos = 16;
                    ypos = ypos + 16;
                }
                else
                //otherwise, increment to the next x position in the current row.
                {
                    xpos = xpos + 16;
                }
            }
            Reset();
        }

        //Initialize
        void Reset()
        {
            nPoints = 0;
            SetScore();
            nDir = 2;
            bSick = false;
            SetHiScore(); 

            //Set every picturebox black
            for (int i = 0; i < 1024; i++)
            {
                grid[i].BackColor = Color.Black;
                grid[i].Image = null;
            }

            //Eliminate all snake segments
            snake.Clear();
            snakedirs.Clear();
            turds.Clear();

            //Make default snake segments
            for (int i = 390; i < (390 + nInitialSnakeLength); i++)
            {
                snake.Add(i);
                snakedirs.Add(nDir);
            }

            //Add the snake to the board
            RefreshSnake();

            //Add a target to the board
            SetRandomTarget();
        }

        //Set a target, or fruit, or whatever, to a random location.
        void SetRandomTarget()
        {
            //Choose a random box
            int n = _r.Next(1024);

            //If the random box is not part of the snake or a turd, make a target
            if (grid[n].BackColor != Color.DarkSeaGreen && grid[n].BackColor != Color.Brown)
            {
                if (bUseImages)
                {
                    Image mouse = Properties.Resources.ResourceManager.GetObject("mouse") as Image;
                    grid[n].Image = mouse;
                }
                grid[n].BackColor = Color.Gray;
            }
            //If it is part of the snake or a turd, try again
            else
            {
                SetRandomTarget();
            }
        }

        //Manage the snake
        void ManageSnake()
        {
            int added;
            //Check if borders kill
            bWarpWalls = checkWarpWalls.Checked;
                //Handle based on direction of movement
                switch (nDir)
                {
                    //Up
                    case 1:
                        //add a new snake segment
                        added = snake.Last() - 32;
                        //if the new segment is out of bounds...
                        if (added < 0)
                        {
                            //and "Warp Walls" are off: Die.
                            if (!bWarpWalls)
                            {
                                GameOver();
                            }
                            //Otherwise: Loop around the opposite side.
                            else
                            {
                                added = added + 1024;
                            }
                        }
                        break;
                    //Down
                    case 3:
                        //Repeat for each possible direction
                        added = snake.Last() + 32;
                        if (added > 1023)
                        {
                            if (!bWarpWalls)
                            {
                                GameOver();
                            }
                            else
                            {
                                added = added - 1024;
                            }
                        }
                        break;
                    //Left
                    case 4:
                        added = snake.Last() - 1;
                        if ((added + 1) % 32 == 0)
                        {
                            if (!bWarpWalls)
                            {
                                GameOver();
                            }
                            else
                            {
                                added = added + 32;
                            }
                        }
                        break;
                    //Right
                    default:
                        added = snake.Last() + 1;
                        if (added % 32 == 0)
                        {
                            if (!bWarpWalls)
                            {
                                GameOver();
                            }
                            else
                            {
                                added = added - 32;
                            }
                        }
                        break;
                }

                if (bRunning)
                {
                    //Add the new segment
                    snake.Add(added);
                    //Remember the direction associated with that segment.
                    snakedirs.Add(nDir);

                    //If you just ate a target, grow by one
                    //Otherwise, remove a segment, and simply "move"
                    if (!bGrow)
                    {
                        grid[snake.First()].BackColor = Color.Black;
                        grid[snake.First()].Image = null;
                        snake.Remove(snake.First());
                        snakedirs.Remove(snakedirs.First());
                    }
                    bGrow = false;
                }
                //Draw the snake on the board
                RefreshSnake();
        }

        void RefreshSnake()
        {
            //Check to make sure we didn't just overlap or eat turd.
            //If so: Die
            if (grid[snake.Last()].BackColor == Color.DarkSeaGreen || //Well Snake Body
                grid[snake.Last()].BackColor == Color.Maroon || //Sick Snake Body
                grid[snake.Last()].BackColor == Color.DarkOliveGreen) //Digesting Mouse segment
            {
                if (bRunning)
                {
                    GameOver();
                }
            }
            //Ate a turd while already sick?
            else if ((grid[snake.Last()].BackColor == Color.Brown) && (bSick))
            {
                if (bRunning)
                {
                    GameOver();
                }
            }
            //If not, move or grow
            else
            {
                foreach (int n in snake)
                {
                    string existingcolor = grid[n].BackColor.Name.ToString();
                    grid[n].Image = null;
                    switch (existingcolor)
                    {
                        //Did you just eat?
                        //If so...
                        case "Gray":
                            //Grow
                            bGrow = true;
                            //Feel Better
                            bSick = false;
                            //Points!
                            nPoints++;
                            if (!bWarpWalls)
                            {
                                //If you're not using warp walls: More Points!
                                nPoints++;
                            }
                            if (checkTurds.Checked)
                            {
                                //If you're dealing with turds: More Points!
                                nPoints++;
                            }
                            //Report new score
                            SetScore();
                            //Replace the mouse with snake body
                            grid[n].BackColor = Color.DarkSeaGreen;
                            if (checkTurds.Checked)
                            {
                                //Digest and make a turd
                                turds.Add(n);
                                if (turds.Count() > nMaxTurds)
                                {
                                    //If you've already got a bunch of turds on the board, remove the oldest
                                    grid[turds.First()].Image = null;
                                    grid[turds.First()].BackColor = Color.Black;
                                    turds.Remove(turds.First());
                                }
                            }
                            //Need a new mouse
                            SetRandomTarget();
                            break;
                        case "Brown":
                            //You get sick when you eat turds
                            bSick = true;
                            break;
                        //If not, just move
                        default:
                            if (!bSick)
                            {
                                //Make snake new segment
                                grid[n].BackColor = Color.DarkSeaGreen;
                                if (!bSickInverts)
                                    _timer1.Interval = nSnakeSpeed;
                            }
                            else
                            {
                                //If you're sick, the snake is red
                                grid[n].BackColor = Color.Maroon;
                                if (!bSickInverts)
                                    _timer1.Interval = nSnakeSpeed / 2;
                            }
                            break;                    
                    }
                }

                foreach (int n in turds)
                {
                    if (!snake.Contains(n))
                    {
                        //Create turds, as long as it's inside the snake
                        if (bUseImages)
                        {
                            Image turd = Properties.Resources.ResourceManager.GetObject("turd") as Image;
                            grid[n].Image = turd;
                        }
                        grid[n].BackColor = Color.Brown;
                    }
                    else
                    {
                        //If it is inside the snake, mark the segment.
                        grid[n].BackColor = Color.DarkOliveGreen;
                    }
                }
                //Add the head and tail graphics
                if (bUseImages)
                {
                    Image head = Properties.Resources.ResourceManager.GetObject("snakehead") as Image;
                    Image tail = Properties.Resources.ResourceManager.GetObject("snaketail") as Image;
                    Image headsick = Properties.Resources.ResourceManager.GetObject("snakeheadsick") as Image;
                    Image tailsick = Properties.Resources.ResourceManager.GetObject("snaketailsick") as Image;
                    //If you don't lock them you get intermittent crashes.
                    lock (head)
                    lock (tail)
                    lock (headsick)
                    lock (tailsick)
            
                    //Flip and rotate the images, based on the direction.
                    if (!bSick)
                    {
                        //Orient the head image face to the most recent direction
                        switch (snakedirs.Last())
                        {
                            case 1:
                                head.RotateFlip(RotateFlipType.Rotate90FlipY);
                                break;
                            case 3:
                                head.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case 4:
                                head.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                break;
                            default:
                                break;
                        }
                        //For the tail, use the direction of the previous segment so it "swings" around turns. Otherwise it disconnects.
                        switch (snakedirs.ElementAt(1))
                        {
                            case 1:
                                tail.RotateFlip(RotateFlipType.Rotate90FlipY);
                                break;
                            case 3:
                                tail.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case 4:
                                tail.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                break;
                            default:
                                break;
                        }
                        grid[snake.Last()].Image = head;
                        grid[snake.First()].Image = tail;

                    }
                    else
                    {
                        //Orient the head to face the most recent direction
                        switch (snakedirs.Last())
                        {
                            case 1:
                                headsick.RotateFlip(RotateFlipType.Rotate90FlipY);
                                break;
                            case 3:
                                headsick.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case 4:
                                headsick.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                break;
                            default:
                                break;
                        }
                        //For the tail, use the direction of the previous segment so it "swings" around turns. Otherwise it disconnects.
                        switch (snakedirs.ElementAt(1))
                        {
                            case 1:
                                tailsick.RotateFlip(RotateFlipType.Rotate90FlipY);
                                break;
                            case 3:
                                tailsick.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case 4:
                                tailsick.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                break;
                            default:
                                break;
                        }
                        grid[snake.Last()].Image = headsick;
                        grid[snake.First()].Image = tailsick;
                    }
                }
            }
        }

        //Dead
        void GameOver()
        {
            bRunning = false;
            _timer1.Stop();
            //If it's a new high score, save it.
            if (nPoints > Properties.Settings.Default.HiScore)
            {
                Properties.Settings.Default.HiScore = nPoints;
                Properties.Settings.Default.Save();
            }
            MessageBox.Show("Dead!");
            Reset();
        }

        //Handle keyboard
        void checkBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //If you're sick controls are inverted.
            //Down goes up, left goes right, etc.

            if (bSick && bSickInverts)
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        if (nDir != 3)
                            nDir = 1;
                        break;
                    case Keys.Up:
                        if (nDir != 1)
                            nDir = 3;
                        break;
                    case Keys.Right:
                        if (nDir != 2)
                            nDir = 4;
                        break;
                    case Keys.Left:
                        if (nDir != 4)
                            nDir = 2;
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Up:
                        if (nDir != 3)
                            nDir = 1;
                        break;
                    case Keys.Down:
                        if (nDir != 1)
                            nDir = 3;
                        break;
                    case Keys.Left:
                        if (nDir != 2)
                            nDir = 4;
                        break;
                    case Keys.Right:
                        if (nDir != 4)
                            nDir = 2;
                        break;
                }
            }

            //Start running the game if you're not running already.
            //Don't start on left; it will result in immediate death
            if ((!bRunning) & (e.KeyCode != Keys.Left))
            {
                Start();
            }
        }

        //Start Timer
        //Gets the snake moving
        void Start()
        {
            bRunning = true;
            _timer1.Start();
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ManageSnake();
            //Timer does not automatically run. It just triggers once each time we start it.
            //So, restart it here.
            _timer1.Start();
        }

        //Write the current score to the window
        private void SetScore()
        {
            if (this.labelScore.InvokeRequired)
            {
                SetScoreCallback d = new SetScoreCallback(SetScore);
                this.Invoke(d);
            }
            else
            {
                this.labelScore.Text = "Score: " + nPoints.ToString();
            }
        }

        //Write the user's all time high score to the window
        private void SetHiScore()
        {
            if (this.labelHiScore.InvokeRequired)
            {
                SetHiScoreCallback d = new SetHiScoreCallback(SetHiScore);
                this.Invoke(d);
            }
            else
            {
                this.labelHiScore.Text = "Hi Score: " + Properties.Settings.Default.HiScore.ToString();
            }
        }

    }
}