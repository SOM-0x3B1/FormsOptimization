using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizationTests
{
    public class Tree
    {
        public Point pos;
        public static int cFrame = 0;

        public List<Bitmap> frames = new List<Bitmap> // a very inefficient example (2.a)
        { 
            Properties.Resources.simple_tree_1,
            Properties.Resources.simple_tree_2,
            Properties.Resources.simple_tree_3,
            Properties.Resources.simple_tree_4
        };        

        public Tree(Point pos) {
            this.pos = pos;
            trees.Add(this);
        }

        public static List<Tree> trees = new List<Tree>();
    }
}
