using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    public class MyNode
    {
        private int id { get; set; }
        private double latitude { get; set; }
        private double longitude { get; set; }
        private string openid { get; set; }

        public MyNode(int new_id)
        {
            id = new_id;
        }

        public MyNode(int new_id, double late, double longe, string new_openid)
        {
            id = new_id;
            latitude = late;
            longitude = longe;
            openid = new_openid;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            MyNode p = obj as MyNode;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return id == p.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }
}
