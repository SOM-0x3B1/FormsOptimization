using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

/* 1. Ne deklaráld újra framenként a Graphics-ot
 * 
 * 2. Ne hozz létre új Bitmapeket, csak ha muszáj
 *   a) A Properties.Resources.<kép> mindig új Bitmap-et hoz létre
 *   b) A vásznat soha se törölt egy új, üres Bitmappel
 *      - használj g.Clear(<szín>)-t, vagy írd felül egy nagy háttérrel (+ 4. és 6. pont)
 *   c) Ha van egy image, amit nem használsz, azonnal .Dispose()-old
 *
 * *3. Ne resize-olj valós időben
 *   a) Néha a C# automatikusan átméretez egy képet, ha nem adtál meg méretet
 *      - add meg minden esetben a kép eredeti dimenzióit
 *
 * 4. Inkább kevesebb nagyot rajzolj, mint nagyon sok kicsit
 *   - a forms simán kirajzol egy fullHD képet, de 200 db 2*2-es pöttybe belehal
 *
 * 5. A for általában gyorsabb, mint a foreach
 *
 * 6. Használj CompositingMode.SourceCopy-t, ha nem áttetsző képet rajzolsz (gyors)
 *   - egyébként a CompositingMode.SourceOver a default (lassú)
 */


namespace OptimizationTests
{
    public partial class Form1 : Form
    {
        public static DateTime _lastTime;
        public static int _framesRendered;
        public static int _fps;

        public static Point mousePos;

        public static Random rnd = new Random();


        public Form1()
        {
            InitializeComponent();                     
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartDrawing();
        }


        private async void StartDrawing()
        {
            for (int i = 0; i < 10; i++)
                new Tree(new Point(rnd.Next(pictureBox1.Width), rnd.Next(pictureBox1.Height)));            

            await Task.Run(() =>
            {
                Level4();
            });
        }


        private void Level1()
        {
            while (true)
            {
                pictureBox1.Invoke(new MethodInvoker(delegate ()
                {
                    mousePos = pictureBox1.PointToClient(Cursor.Position);

                    pictureBox1.Image = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height); // 2.b
                    Graphics g = Graphics.FromImage(pictureBox1.Image); // 1.

                    for (int x = 0; x < pictureBox1.Width; x += 60)
                        for (int y = 0; y < pictureBox1.Height; y += 60)
                            g.DrawImage(new Bitmap(Properties.Resources.land, 60, 60), x, y); // 2.; 2.a; 3.; 3.a; 4.

                    g.DrawImage(new Bitmap(Properties.Resources.Player_Walking_1, 300, 100), mousePos); //2., 2.a; 3.

                    foreach (var tree in Tree.trees) // 5.
                        g.DrawImage(new Bitmap(tree.frames[Tree.cFrame], 60, 120), tree.pos); // 2.; 3.; 4.

                    Tree.cFrame++;
                    if (Tree.cFrame > 3)
                        Tree.cFrame = 0;

                    pictureBox1.Refresh();
                }));

                UpdateFPS();
            }
        }

        private void Level2() // attempts to dispose of bitmaps ==> 2.a
        {
            while (true)
            {
                pictureBox1.Invoke(new MethodInvoker(delegate ()
                {
                    mousePos = pictureBox1.PointToClient(Cursor.Position);

                    if (pictureBox1.Image != null)
                        pictureBox1.Image.Dispose();

                    pictureBox1.Image = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height); // 2.b
                    Graphics g = Graphics.FromImage(pictureBox1.Image); // 1.

                    Bitmap land = new Bitmap(Properties.Resources.land, 60, 60); // 2.a
                    for (int x = 0; x < pictureBox1.Width; x += 60)
                        for (int y = 0; y < pictureBox1.Height; y += 60)
                            g.DrawImage(land, x, y); // 3.; 3.a; 4.
                    land.Dispose();

                    Bitmap player = new Bitmap(Properties.Resources.Player_Walking_1, 300, 100); // 2.a
                    g.DrawImage(player, mousePos); // 3.
                    player.Dispose();

                    foreach (var tree in Tree.trees) // 5.
                    {
                        Bitmap treeBitmap = new Bitmap(tree.frames[Tree.cFrame], 60, 120); // 2.a
                        g.DrawImage(treeBitmap, tree.pos); // 3.; 4.
                        treeBitmap.Dispose();
                    }

                    Tree.cFrame++;
                    if (Tree.cFrame > 3)
                        Tree.cFrame = 0;

                    pictureBox1.Refresh();
                }));

                UpdateFPS();
            }
        }

        private void Level3() // fixes memory leak, uses g.Clear() and gets rid of resizes
        {
            Bitmap land = new Bitmap(Properties.Resources.land, 60, 60);
            Bitmap player = Properties.Resources.Player_Walking_1;

            pictureBox1.Image = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);

            using (Graphics g = Graphics.FromImage(pictureBox1.Image))
            {
                while (true)
                {
                    pictureBox1.Invoke(new MethodInvoker(delegate ()
                    {
                        mousePos = pictureBox1.PointToClient(Cursor.Position);

                        g.Clear(Color.White);

                        for (int x = 0; x < pictureBox1.Width; x += 60)
                            for (int y = 0; y < pictureBox1.Height; y += 60)
                                g.DrawImage(land, x, y, land.Width, land.Height); // 4.

                        g.DrawImage(player, mousePos.X, mousePos.Y, player.Width, player.Height);

                        foreach (var tree in Tree.trees) // 5.
                        {
                            Bitmap frame = tree.frames[Tree.cFrame];
                            g.DrawImage(frame, tree.pos.X, tree.pos.Y, frame.Width, frame.Height); // 4.                            
                        }
                        Tree.cFrame++;
                        if (Tree.cFrame > 3)
                            Tree.cFrame = 0;

                        pictureBox1.Refresh();
                    }));

                    UpdateFPS();
                }
            }
        }

        private void Level4() // drastically improves performance by grouping tiles into one image & using CompositingMode
        {
            Bitmap land = new Bitmap(Properties.Resources.land, 60, 60);
            Bitmap player = Properties.Resources.Player_Walking_1;

            Bitmap background = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            using (Graphics g = Graphics.FromImage(background))
            {
                for (int x = 0; x < pictureBox1.Width; x += 60)
                    for (int y = 0; y < pictureBox1.Height; y += 60)
                        g.DrawImage(land, x, y, land.Width, land.Height);
            }

            pictureBox1.Image = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);

            using (Graphics g = Graphics.FromImage(pictureBox1.Image))
            {
                while (true)
                {
                    pictureBox1.Invoke(new MethodInvoker(delegate ()
                    {
                        mousePos = pictureBox1.PointToClient(Cursor.Position);

                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.DrawImage(background, 0, 0, background.Width, background.Height);

                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        g.DrawImage(player, mousePos.X, mousePos.Y, player.Width, player.Height);

                        for (int i = 0; i < Tree.trees.Count; i++)
                        {
                            Tree tree = Tree.trees[i];
                            Bitmap frame = tree.frames[Tree.cFrame];
                            g.DrawImage(frame, tree.pos.X, tree.pos.Y, frame.Width, frame.Height); // 4.
                            
                        }
                        Tree.cFrame++;
                        if (Tree.cFrame > 3)
                            Tree.cFrame = 0;

                        pictureBox1.Refresh();
                    }));

                    UpdateFPS();
                }
            }
        }

        private void Level5() // creates consistent (but in this case lover) fps with any number of animated trees by grouping them together too)
        {
            Bitmap land = new Bitmap(Properties.Resources.land, 60, 60);
            Bitmap player = Properties.Resources.Player_Walking_1;

            Bitmap background = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            using (Graphics g = Graphics.FromImage(background))
            {
                for (int x = 0; x < pictureBox1.Width; x += 60)
                    for (int y = 0; y < pictureBox1.Height; y += 60)
                        g.DrawImage(land, x, y, land.Width, land.Height);
            }

            List<Bitmap> forest = new List<Bitmap>();
            for (int i = 0; i < 4; i++)
            {
                forest.Add(new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height));
                using (Graphics g = Graphics.FromImage(forest[i]))
                {
                    for (int j = 0; j < Tree.trees.Count; j++)
                    {
                        Tree tree = Tree.trees[j];
                        Bitmap frame = tree.frames[Tree.cFrame];
                        g.DrawImage(frame, tree.pos.X, tree.pos.Y, frame.Width, frame.Height);                        
                    }
                    Tree.cFrame++;
                    if (Tree.cFrame > 3)
                        Tree.cFrame = 0;
                }
            }

            pictureBox1.Image = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);

            using (Graphics g = Graphics.FromImage(pictureBox1.Image))
            {
                while (true)
                {
                    pictureBox1.Invoke(new MethodInvoker(delegate ()
                    {
                        mousePos = pictureBox1.PointToClient(Cursor.Position);

                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.DrawImage(background, 0, 0, background.Width, background.Height);

                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        g.DrawImage(player, mousePos.X, mousePos.Y, player.Width, player.Height);

                        g.DrawImage(forest[Tree.cFrame], 0, 0, forest[Tree.cFrame].Width, forest[Tree.cFrame].Height);

                        pictureBox1.Refresh();
                    }));

                    Tree.cFrame++;
                    if (Tree.cFrame > 3)
                        Tree.cFrame = 0;

                    UpdateFPS();
                }
            }
        }


        private void UpdateFPS()
        {
            _framesRendered++;

            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                _fps = _framesRendered;
                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }

            SetText(_fps.ToString());
        }

        private void SetText(string text)
        {
            if (fpsLabel.InvokeRequired)
            {
                fpsLabel.Invoke(new MethodInvoker(
                delegate ()
                {
                    fpsLabel.Text = text;
                }));
            }
            else
                fpsLabel.Text = text;
        }        
    }
}
