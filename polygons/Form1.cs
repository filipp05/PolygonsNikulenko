using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;

namespace polygons
{
    public partial class Form1 : Form
    {
        Shape C = new Circle(new Point());

        List<Shape> shapes;
        enum ShapeTypes { круг, треугольник, квадрат }

        ShapeTypes selectedShapeType;

        RadiusForm radiusForm;

        BinaryFormatter bf;
        FileStream fs;
        FormData fd;

        Stack<FormData> undos;
        Stack<FormData> redos;

        int coursorX = -1;
        int coursorY = -1;
        bool isMousePressed = false;

        public bool formSaved = false;
        public bool formChanged = false;
        public string filePath = null;

        public Form1()
        {
            InitializeComponent();
            shapes = new List<Shape>();
            selectedShapeType = ShapeTypes.круг;
            chart1.Visible = false;
            undos = new Stack<FormData>();
            redos = new Stack<FormData>();
            отменитьToolStripMenuItem.Enabled = false;
            вернутьToolStripMenuItem.Enabled = false;


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DoubleBuffered = true;
            поОпределениюпоУмолчаниюToolStripMenuItem.Checked = true;
        }


        public void OnRadiusChanging(object sender, RadiusEventArgs e)
        {
            Shape.Radius = e.radius;
            Refresh();
        }

        public void OnRadiusChanged(object sender, RadiusEventArgs e)
        {
            PushStateToUndo();
            formChanged = true;
        }


        public static int IsOnSameSide(Point p01, Point p02, Point p1, Point p2) // 1 - точки с одной стороны 0 - точкис лежат на прямой -1 - точки лежат по разные стороны
        {
            if (p01.X - p02.X == 0)
            {
                return (int)Math.Sign((p01.X - p1.X) * (p01.X - p2.X));
            }
            double k = (double)(p01.Y - p02.Y) / (p01.X - p02.X);
            double b = (double)(p01.Y - k * p01.X);
            return (int)Math.Sign((p1.Y - p1.X * k - b) * (p2.Y - p2.X * k - b));
        }


        public static int GetPointSignPosition(Point p1, Point p2, Point p)
        {
            int sign = (p1.X - p.X) * (p2.Y - p1.Y) - (p2.X - p1.X) * (p1.Y - p.Y);

            if (sign == 0) return sign;

            return sign / Math.Abs(sign);
        } 


        void clearMenuStrip(int itemGroup) // 1 - тип фигуры 2 - алгоритм
        {
            switch (itemGroup)
            {
                case 1:
                    треугольникToolStripMenuItem.Checked = false;
                    квадратToolStripMenuItem.Checked = false;
                    кругToolStripMenuItem.Checked = false;
                    break;
                case 2:
                    поОпределениюпоУмолчаниюToolStripMenuItem.Checked = false;
                    джарвисToolStripMenuItem.Checked = false;
                    break;
                case 3:
                    даToolStripMenuItem.Checked = false;
                    нетToolStripMenuItem.Checked = false;
                    break;
            }
        }


        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            foreach (Shape s in shapes)
                {
                    s.Draw(g);
                }
            
            foreach (Shape s in shapes)
            {
                s.is_outside_shell = false;
            }

            if (поОпределениюпоУмолчаниюToolStripMenuItem.Checked)
            {
                Pen pen = new Pen(Color.Black, Shape.Thickness);

                for (int i = 0; i < shapes.Count; i++)
                {
                    for (int j = i + 1; j < shapes.Count; j++)
                    {
                        if (CheckPointsSide(shapes[i], shapes[j], shapes) && shapes.Count > 2)
                        {
                            e.Graphics.DrawLine(pen, shapes[i].Position.X, shapes[i].Position.Y, shapes[j].Position.X, shapes[j].Position.Y);
                        }
                    }
                }
            }

            if (джарвисToolStripMenuItem.Checked)
            {
                if (shapes.Count > 2)
                {
                    Point C;
                    Point checkPoint = shapes[0].Position;
                    Point A, B;

                    foreach (Shape s in shapes)
                    {
                        if (s.Position.Y < checkPoint.Y)
                        {
                            checkPoint = s.Position;
                        }
                    }

                    A = checkPoint;
                    B = A;
                    B.X -= 100;

                    do
                    {
                        C = getShapePoint(A, B, shapes);

                        e.Graphics.DrawLine(new Pen(Color.Black, 2), A, C);

                        B = A;
                        A = C;
                    }
                    while (C != checkPoint);
                }
                
            }
        }


        Point getShapePoint(Point A, Point B, List<Shape> shapes)
        {
            
            double maxAngleCos = 1;
            double angleCos;

            foreach (Shape s in shapes)
            {
                if (s.Position != A) //&& s.Position != B
                {
                    angleCos = getCosBy3Point(A, B, s.Position);
                    if (angleCos < maxAngleCos)
                    {
                        maxAngleCos = angleCos;
                        C = s;
                    }
                }
            }

            C.is_outside_shell = true;

            return C.Position;
        }

        double getCosBy3Point(Point A, Point B, Point C)
        {
            // a**2 = b**2 + c**2 - 2*cos(a)*b*c; cos(a)=(b**2 + c**2 - a**2)/2*b*c

            double ab = Math.Sqrt(Math.Pow(B.X - A.X, 2) + Math.Pow(B.Y - A.Y, 2));
            double ac = Math.Sqrt(Math.Pow(C.X - A.X, 2) + Math.Pow(C.Y - A.Y, 2));
            double cb = Math.Sqrt(Math.Pow(B.X - C.X, 2) + Math.Pow(B.Y - C.Y, 2));

            return (ab * ab + ac * ac - cb * cb) / (2 * ab * ac);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            
            if (e.Button == MouseButtons.Left)
            {
                isMousePressed = true;
                bool flag = true;

                foreach(Shape s in shapes)
                {
                    if (s.IsInside(e.X, e.Y))
                    {
                        flag = false;
                        break;
                    }
                }

                bool isInsideShell = false;

                if (shapes.Count > 2)
                {
                    isInsideShell = true;
                    //int sign = IsOnSameSide(shapes[0].Position, shapes[1].Position, shapes[2].Position, new Point(e.X, e.Y));

                    
                    for (int i = 0; i < shapes.Count; i++)
                    {
                        for (int j = i + 1; j < shapes.Count; j++)
                        {
                            int k;
                            for (k = 0; k < shapes.Count; k++)
                            {
                                if (k != i && k != j) break;
                            }
                            if (CheckPointsSide(shapes[i], shapes[j], shapes) &&  IsOnSameSide(shapes[i].Position, shapes[j].Position, shapes[k].Position, new Point(e.X, e.Y)) != 1)
                            {
                                isInsideShell = false;
                                break;
                            }
                        }

                        if (!isInsideShell)
                        {
                            break;
                        }
                    }
                }


                if (flag && !isInsideShell)
                {
                    PushStateToUndo();

                    switch (selectedShapeType)
                    {
                        case ShapeTypes.квадрат:
                            shapes.Add(new Square(new Point(e.X, e.Y)));
                            break;
                        case ShapeTypes.треугольник:
                            shapes.Add(new Triangle(new Point(e.X, e.Y)));
                            break;
                        case ShapeTypes.круг:
                            shapes.Add(new Circle(new Point(e.X, e.Y)));
                            break;
                    }
                    formChanged = true;

                    Refresh();
                }

                else if (!flag)
                {
                    PushStateToUndo();

                    foreach (Shape s in shapes)
                    {
                        if (s.IsInside(e.X, e.Y))
                        {

                            s.IsActive = true;
                            s.isHold = true;
                            s.DeltaX = e.X - s.Position.X;
                            s.DeltaY = e.Y - s.Position.Y;
                        }
                    }
                }

                else if (isInsideShell)
                {
                    PushStateToUndo();
                }
            }

            else if(e.Button == MouseButtons.Right)
            {
                PushStateToUndo();
                for(int i = shapes.Count - 1; i >= 0; i--)
                {
                    if (shapes[i].IsInside(e.X, e.Y))
                    {
                        shapes.RemoveAt(i);
                        break;
                    }
                }
                formChanged = true;
                Refresh();
            }


            

            
            
            
            
            
        }


        private void PushStateToUndo()
        {
            FormData state = new FormData(shapes);
            undos.Push(state);
            отменитьToolStripMenuItem.Enabled = true;
            redos.Clear();

            вернутьToolStripMenuItem.Enabled = false;
        }


        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            bool isActiveShapeFound = false;

            foreach (Shape s in shapes)
            {
                if (s.isHold && s.IsActive)
                {
                    s.Position = new Point(e.X - s.DeltaX, e.Y - s.DeltaY);
                    this.Refresh();
                    formChanged = true;
                    isActiveShapeFound = true;
                }
                
            }
            
            if (!isMousePressed || isActiveShapeFound)
            {
                return;
            }

            if (coursorX == -1 && coursorY == -1)
            {
                coursorX = e.X;
                coursorY = e.Y;
                return;
            }

            foreach (Shape s in shapes)
            {
                s.Position = new Point(s.Position.X - (coursorX - e.X), s.Position.Y - (coursorY - e.Y));
            }

            coursorX = e.X;
            coursorY = e.Y;
            this.Refresh();
            formChanged = true;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            isMousePressed = false;

            coursorY = -1;
            coursorX = -1;

            foreach (Shape s in shapes)
            {
                s.IsActive = false;
                s.isHold = false;
            }

            if (shapes.Count > 3)
            {
                for (int i = 0; i < shapes.Count;)
                {
                    if (!shapes[i].is_outside_shell)
                    {
                        shapes.RemoveAt(i);
                        Refresh();
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void треугольникToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedShapeType = ShapeTypes.треугольник;
            clearMenuStrip(1);
            треугольникToolStripMenuItem.Checked = true;
        }

        private void квадратToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedShapeType = ShapeTypes.квадрат;
            clearMenuStrip(1);
            квадратToolStripMenuItem.Checked = true;
        }

        private void кругToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectedShapeType = ShapeTypes.круг;
            clearMenuStrip(1);
            кругToolStripMenuItem.Checked = true;
        }


        bool CheckPointsSide(Shape s1, Shape s2, List<Shape> shapes)
        {
            Point p1 = s1.Position;
            Point p2 = s2.Position;
            bool final_result = true; // 
            int check_result = 1000;

            if (p1.X == p2.X)
            {
                for (int _ = 0; _ < shapes.Count; _++)
                {
                    Point p3 = shapes[_].Position;
                    if (p1 != p3 && p2 != p3)
                    {
                        int result = (int)Math.Sign(p3.X - p1.X);                                  // 1, точка над прямой
                        if (check_result == 1000)
                        {
                            
                            check_result = result;
                        }
                        else if (check_result != result)
                        {
                            final_result = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < shapes.Count; i++)
                {
                    Point p3 = shapes[i].Position;
                    if (p1 != p3 && p2 != p3)
                    {
                        int status = IsOnSameSide(p1, p2, p3, new Point(p1.X, p1.Y + 1));
                        if (check_result == 1000)
                        {   
                            check_result = status;
                        }
                        else if (check_result != status)
                        {
                            final_result = false;
                            break;
                        }
                    }
                }
            }

            if (final_result)
            {
                s1.is_outside_shell = true;
                s2.is_outside_shell = true;
            }

            return final_result;
        }

        private void поОпределениюпоУмолчаниюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearMenuStrip(2);
            поОпределениюпоУмолчаниюToolStripMenuItem.Checked = true;

        }

        private void джарвисToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearMenuStrip(2);
            джарвисToolStripMenuItem.Checked = true;
        }

        private void даToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearMenuStrip(3);
            даToolStripMenuItem.Checked = true;
            chart1.Visible = true;

            Random r = new Random();
            List<Shape> testshapes = new List<Shape>();
            List<double> timeDataByDefault = new List<double>();
            List<double> timeDataByJarvis = new List<double>();
            Stopwatch timer = new Stopwatch();

            int pointCount = 1500;
            
            Point C;
            Point checkPoint;
            Point A, B;

            Series defaultPoints = new Series("Алгоритм 'По умалчанию'");
            Series jarvisPoints = new Series("Алгоритм 'Джарвис'");

            defaultPoints.ChartType = SeriesChartType.Line;
            jarvisPoints.ChartType = SeriesChartType.Line;


            for (int k = 3; k <= pointCount; k+=200)
            {
                for (int i = 3; i < pointCount; i+=300)
                {
                    testshapes.Add(new Circle(new Point(r.Next(0, this.Width), r.Next(0, this.Height))));
                }

                checkPoint = testshapes[0].Position;

                timer.Start();

                for (int i = 0; i < testshapes.Count; i++)
                {
                    for (int j = i + 1; j < testshapes.Count; j++)
                    {
                        CheckPointsSide(testshapes[i], testshapes[j], testshapes);
                    }
                }

                timer.Stop();
                timeDataByDefault.Add(timer.Elapsed.TotalSeconds);
                timer.Reset();

                for (int i = 0; i < testshapes.Count; i++)
                {
                    testshapes[i].is_outside_shell = false;
                }

                /////////////////////////////////////////////
                
                timer.Start();

                

                foreach (Shape s in testshapes)
                {
                    if (s.Position.Y < checkPoint.Y)
                    {
                        checkPoint = s.Position;
                    }
                }

                A = checkPoint;
                B = A;
                B.X -= 100;

                do
                {
                    C = getShapePoint(A, B, testshapes);

                    B = A;
                    A = C;
                }
                while (C != checkPoint);

                timer.Stop(); 
                timeDataByJarvis.Add(timer.Elapsed.TotalSeconds);
                timer.Reset();
            }

            for (int i = 0; i < timeDataByDefault.Count; i++)
            {
                defaultPoints.Points.AddXY(i * 200, timeDataByDefault[i]);
                jarvisPoints.Points.AddXY(i * 200, timeDataByJarvis[i]);
            }

            chart1.Series.Add(defaultPoints);
            chart1.Series.Add(jarvisPoints);
           

        }

        private void нетToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearMenuStrip(3);
            нетToolStripMenuItem.Checked = true;
            chart1.Visible = false;
            chart1.Series.Clear();
        }

        private void радиусToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (radiusForm == null || radiusForm.IsDisposed) // Возвращает значение, указывающее, был ли удален элемент управления.
            {
                radiusForm = new RadiusForm();
                radiusForm.Show();
            }

            if (radiusForm.WindowState != FormWindowState.Maximized)
            {
                radiusForm.BringToFront();
                radiusForm.WindowState = FormWindowState.Normal;
            }

            radiusForm.RadiusChanging += OnRadiusChanging;
            radiusForm.RadiusChanged += OnRadiusChanged;

            

           // radiusForm.WindowState = FormWindowState.Minimized;
           // radiusForm.BringToFront();







        }

        private void цветToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PushStateToUndo();
            ColorDialog MyDialog = new ColorDialog();
            MyDialog.AllowFullOpen = true;
            MyDialog.ShowHelp = true;
            MyDialog.Color = Shape.FillColor;

            if (MyDialog.ShowDialog() == DialogResult.OK) 
                Shape.FillColor = MyDialog.Color;
            
            Refresh();
            formChanged = true;
        }

        private void OpenFileClick(object sender, EventArgs e)
        {
            SaveChanges(sender, e);

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (radiusForm != null) radiusForm.Close();
                openFileDialog.InitialDirectory = Assembly.GetExecutingAssembly().Location;
                openFileDialog.RestoreDirectory = false;
                openFileDialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
                openFileDialog.FileName = ".bin";
                if (openFileDialog.ShowDialog() == DialogResult.OK && openFileDialog.FileName != filePath)
                {
                    filePath = openFileDialog.FileName;
                    this.Text = filePath;
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    FormData obj = (FormData)bf.Deserialize(fs);
                    shapes = obj.Polygons;
                    Shape.Radius = obj.Radius;
                    Shape.FillColor = obj.Color;
                    fs.Close();
                    Refresh();
                }
            }
        }

        void SaveChanges(Object sender, EventArgs e)
        {
            if (formChanged)
            {
                DialogResult result = MessageBox.Show("Сохранить изменения?", "Фигура изменена", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (filePath != null) SaveFileClick(sender, e);

                    else SaveFileAsClick(sender, e);

                }
            }
        }

        private void NewFileClick(object sender, EventArgs e)
        {
            SaveChanges(sender, e);
            if (radiusForm != null) radiusForm.Close();
            clearMenuStrip(1);
            кругToolStripMenuItem.Checked = true;
            selectedShapeType = ShapeTypes.круг;

            shapes = new List<Shape>();
            Shape.FillColor = Color.Black;
            Shape.Radius = 40;
            filePath = null;
            formChanged = false;
            formSaved = false;
            this.Text = "Новая фигура";


            Refresh();
        }

        private void SaveFileClick(object sender, EventArgs e)
        {
            if (filePath != null) SaveState(filePath);
            else  SaveFileAsClick(sender, e);
        }

        private void SaveFileAsClick(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Assembly.GetExecutingAssembly().Location;
                saveFileDialog.RestoreDirectory = false;
                saveFileDialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
                saveFileDialog.FileName = ".bin";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    formChanged = false;
                    
                    SaveState(filePath);
                    this.Text = filePath;
                }
            }
        }


        void SaveState(string path)
        {
            FormData obj = new FormData(this.shapes);
            bf = new BinaryFormatter();
            fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            bf.Serialize(fs, obj);
            //  bf.Serialize(fs, Shape.Radius);
            //  bf.Serialize(fs, Shape.FillColor);
            fs.Close();
            formChanged = false;
            formSaved = true;

        }


        public FormData LoadState(string path)
        {
            bf = new BinaryFormatter();
            fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            return (FormData)bf.Deserialize(fs);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (formChanged) SaveChanges(sender, e);
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            redos.Push(new FormData(shapes));
            вернутьToolStripMenuItem.Enabled = true;
            FormData state = undos.Pop();

            SetState(state);

            if (undos.Count == 0) отменитьToolStripMenuItem.Enabled = false;

        }

        private void вернутьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            undos.Push(new FormData(shapes));

            FormData state = redos.Pop();

            SetState(state);

            if (redos.Count == 0) вернутьToolStripMenuItem.Enabled = false;
        }


        void SetState(FormData state)
        {
            shapes = state.Polygons;
            Shape.Radius = state.Radius;
            Shape.FillColor = state.Color;

            Refresh();
        }
    }

    


    public class RadiusEventArgs : EventArgs
    {
        public int radius;

        public RadiusEventArgs(int radius) : base()
        {
            this.radius = radius;
        }
    }

    public delegate void RadiusEventHandler(object sender, RadiusEventArgs e);

    [Serializable]
    public class FormData
    {
        List<Shape> _polygons;
        public List<Shape> Polygons { get { return _polygons; } }

        int _radius;
        public int Radius { get { return _radius; } }

        Color _color;
        public Color Color { get { return _color; } }


        public FormData(List<Shape> polygons)
        {
            this._polygons = new List<Shape>();
            polygons.ForEach((polygon) => { this._polygons.Add(polygon.Clone()); });
            this._radius = Shape.Radius;
            this._color = Shape.FillColor;
        }
    }


    /*public abstract class Operation
    {
        public abstract void Undo();

        public abstract void Redo();
    }*/
}
