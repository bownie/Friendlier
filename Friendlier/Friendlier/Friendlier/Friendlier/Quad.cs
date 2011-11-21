using System;  
using Microsoft.Xna.Framework;  
using Microsoft.Xna.Framework.Graphics;  
 
namespace Xyglo  
{
    /// <summary>
    /// For this class thanks to:
    /// 
    /// http://forums.create.msdn.com/forums/p/12089/98958.aspx
    /// 
    /// </summary>
    public class Quad
    {
        # region start

        private VertexPositionNormalTexture [] _vertices ;  
        public VertexPositionNormalTexture [] Vertices
        {  
            get { return _vertices; }  
            set { _vertices = value; }  
        }  
 
        private int [] _indexes;  
        public int [] Indexes  
        {  
            get { return _indexes; }  
            set { _indexes = value; }  
        }  
 
        private Vector3 _origin;  
        public Vector3 Origin  
        {  
            get { return _origin; }  
            set { _origin = value; }  
        }  
 
        private Vector3 _normal;  
        public Vector3 Normal  
        {  
            get { return _normal; }  
            set { _normal = value; }  
        }  
 
        private Vector3 _up;  
        public Vector3 Up  
        {  
            get { return _up; }  
            set { _up = value; }  
        }  
 
        private Vector3 _left;  
        public Vector3 Left  
        {  
            get { return _left; }  
            set { _left = value; }  
        }  
 
        private Vector3 _upperLeft;  
        public Vector3 UpperLeft  
        {  
            get { return _upperLeft; }  
            set { _upperLeft = value; }  
        }  
 
        private Vector3 _upperRight;  
        public Vector3 UpperRight  
        {  
            get { return _upperRight; }  
            set { _upperRight = value; }  
        }  
 
        private Vector3 _lowerLeft;  
        public Vector3 LowerLeft  
        {  
            get { return _lowerLeft; }  
            set { _lowerLeft = value; }  
        }  
 
        private Vector3 _lowerRight;  
        public Vector3 LowerRight  
        {  
            get { return _lowerRight; }  
            set { _lowerRight = value; }  
        }
        #endregion  
 
        public Quad(Vector3 origin, Vector3 normal, Vector3 up, float width, float height)  
        {  
            Vertices = new VertexPositionNormalTexture[4];  
            Indexes = new int[6];  
            Origin = origin;  
            Normal = normal;  
            Up = up;  
 
            // Calculate the quad corners  
            Left = Vector3.Cross(normal, Up);  
            Vector3 uppercenter = (Up * height / 2) + origin;  
            UpperLeft = uppercenter + (Left * width / 2);  
            UpperRight = uppercenter - (Left * width / 2);  
            LowerLeft = UpperLeft - (Up * height);  
            LowerRight = UpperRight - (Up * height);  
 
            FillVertices();  
        }  
 
        private void FillVertices()  
        {  
            // Fill in texture coordinates to display full texture  
            // on quad  
            Vector2 textureUpperLeft = new Vector2(0.0f, 0.0f);  
            Vector2 textureUpperRight = new Vector2(1.0f, 0.0f);  
            Vector2 textureLowerLeft = new Vector2(0.0f, 1.0f);  
            Vector2 textureLowerRight = new Vector2(1.0f, 1.0f);  
 
            // Provide a normal for each vertex  
            for (int i = 0; i < Vertices.Length; i++)  
            {  
                Vertices[i].Normal = Normal;  
            }  
 
            // Set the position and texture coordinate for each  
            // vertex  
            Vertices[0].Position = LowerLeft;  
            Vertices[0].TextureCoordinate = textureLowerLeft;  
            Vertices[1].Position = UpperLeft;  
            Vertices[1].TextureCoordinate = textureUpperLeft;  
            Vertices[2].Position = LowerRight;  
            Vertices[2].TextureCoordinate = textureLowerRight;  
            Vertices[3].Position = UpperRight;  
            Vertices[3].TextureCoordinate = textureUpperRight;  
 
            // Set the index buffer for each vertex, using  
            // clockwise winding  
            Indexes[0] = 0;  
            Indexes[1] = 1;  
            Indexes[2] = 2;  
            Indexes[3] = 2;  
            Indexes[4] = 1;  
            Indexes[5] = 3;  
 
        }  
    }  
} 