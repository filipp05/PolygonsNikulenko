using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace polygons
{
    [Serializable]
    public abstract class Shape : ICloneable
    {
        protected const float thickness = 3f;

        public enum status { True, False, No }

        static int radius;
        protected Point position;

        public bool IsActive { get; set; }
        public bool isHold { get; set; }
        public int DeltaX { get; set; }
        public int DeltaY { get; set; }
        public bool is_outside_shell { get; set; }

        public static float Thickness
        {
            get
            {
                return thickness;
            }
        }

        public Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }



        static public Color FillColor { get; set; }

        static Shape()
        {
            radius = 40;
            FillColor = Color.Black;
        }


        static public int Radius
        {
            get
            {
                return radius;
            }

            set
            {
                if (value <= 0) throw new Exception("Радиус не может быть отрицательным");
                radius = value;
            }
        }


        public abstract void Draw(Graphics graphics);
        public abstract bool IsInside(int pointerX, int pointerY);

        public Shape Clone()
        {
            return (Shape)this.MemberwiseClone();
        }

        object ICloneable.Clone() { return Clone(); }
    }

    [Serializable]
    class Triangle : Shape
    {
        public Triangle(Point position)
        {
            this.position = position;
        }


        public static int Side
        {
            get { return (int)(Radius * Math.Sqrt(3)); }
        }

        public override bool IsInside(int pointerX, int pointerY)
        {
            Point p = new Point(pointerX, pointerY);
            int r = Radius / 2;
            Point[] coords = { new Point(Position.X, Position.Y - r), new Point(Position.X - Side / 2, Position.Y + r), new Point(position.X + Side / 2, Position.Y + r) };
            return IsOnSameSide(coords[0], coords[1], coords[2], p) >= 0 && IsOnSameSide(coords[2], coords[1], coords[0], p) >= 0 &&
                IsOnSameSide(coords[0], coords[2], coords[1], p) >= 0;
        }
        public int IsOnSameSide(Point p01, Point p02, Point p1, Point p2)
        {
            if (p01.X - p02.X == 0)
            {
                return (int)Math.Sign((p01.X - p1.X) * (p01.X - p2.X));
            }
            double k = (p01.Y - p02.Y) / (p01.X - p02.X);
            double b = p01.Y - k * p01.X;
            return (int)Math.Sign((p1.Y - p1.X * k - b) * (p2.Y - p2.X * k - b));
        }

        /*public override void Draw(Graphics graphics)
        {
            double height = Side * Math.Sin(Math.PI / 3);

            Point[] points = {
            new Point(position.X - Side / 2),
            new Point(),
            new Point()
            };

            Pen pen = new Pen(FillColor, thickness);

            Rectangle rect = new Rectangle(position.X - Side / 2, position.Y - Side / 2, Side, Side);

            graphics.DrawPolygon(pen, points);
        }*/


        public override void Draw(Graphics g)
        {
            int r = Radius / 2;
            double A = Radius * Math.Sqrt(3);
            Pen pen = new Pen(FillColor, thickness);
            PointF[] points = { new PointF(position.X, position.Y - Radius), new PointF(position.X - (int)A / 2, position.Y + r), new PointF(position.X + (int)A / 2, position.Y + r) };
            g.DrawPolygon(pen, points);
        }
    }

    [Serializable]
    class Square : Shape
    {
        public Square(Point position)
        {
            this.position = position;
        }


        public static int Side
        {
            get { return (int)(Radius * Math.Sqrt(2)); }
        }

        public override bool IsInside(int pointerX, int pointerY)
        {
            int right = position.X + Side / 2;
            int left = position.X - Side / 2;

            int up = position.Y - Side / 2;
            int down = position.Y + Side / 2;
            //
            if (left < pointerX && pointerX < right)
                if (up < pointerY && pointerY < down)
                    return true;

            return false;
        }

        public override void Draw(Graphics graphics)
        {
            Pen pen = new Pen(FillColor, thickness);

            Rectangle rect = new Rectangle(position.X - Side / 2, position.Y - Side / 2, Side, Side);

            graphics.DrawRectangle(pen, rect);
        }
    }
    
    [Serializable]
    class Circle : Shape
    {
        public Circle(Point position)
        {
            this.position = position;
        }


        public override bool IsInside(int pointerX, int pointerY)
        {
            if (Math.Pow(position.X - pointerX, 2) + Math.Pow(position.Y - pointerY, 2) < Math.Pow(Radius, 2))
                return true;

            return false;
        }

        public override void Draw(Graphics graphics)
        {
            Pen pen = new Pen(FillColor, thickness);

            Rectangle circle = new Rectangle(position.X - Radius, position.Y - Radius, Radius * 2, Radius * 2);

            graphics.DrawEllipse(pen, circle);
        }
    }
}
