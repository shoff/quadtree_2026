using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace QuadtreeComponent
{
    /// <summary>
    /// A simple renderable mesh that replaces XNA Model for content-pipeline-free rendering.
    /// </summary>
    public class SimpleMesh
    {
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private int _primitiveCount;
        private BasicEffect _effect;

        public BasicEffect Effect => _effect;

        public SimpleMesh(GraphicsDevice graphicsDevice, VertexPositionNormalTexture[] vertices, short[] indices)
        {
            _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Length, BufferUsage.None);
            _vertexBuffer.SetData(vertices);
            _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.None);
            _indexBuffer.SetData(indices);
            _primitiveCount = indices.Length / 3;
            _effect = new BasicEffect(graphicsDevice);
            _effect.EnableDefaultLighting();
        }

        public void Draw(Matrix world, Matrix view, Matrix projection)
        {
            _effect.World = world;
            _effect.View = view;
            _effect.Projection = projection;

            var gd = _effect.GraphicsDevice;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.SetVertexBuffer(_vertexBuffer);
                gd.Indices = _indexBuffer;
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
            }
        }

        public void CopyEffectFrom(BasicEffect source)
        {
            _effect.AmbientLightColor = source.AmbientLightColor;
            _effect.SpecularColor = source.SpecularColor;
            _effect.SpecularPower = source.SpecularPower;

            _effect.DirectionalLight0.Enabled = source.DirectionalLight0.Enabled;
            _effect.DirectionalLight0.DiffuseColor = source.DirectionalLight0.DiffuseColor;
            _effect.DirectionalLight0.Direction = source.DirectionalLight0.Direction;
            _effect.DirectionalLight0.SpecularColor = source.DirectionalLight0.SpecularColor;

            _effect.DirectionalLight1.Enabled = source.DirectionalLight1.Enabled;
            _effect.DirectionalLight1.DiffuseColor = source.DirectionalLight1.DiffuseColor;
            _effect.DirectionalLight1.Direction = source.DirectionalLight1.Direction;
            _effect.DirectionalLight1.SpecularColor = source.DirectionalLight1.SpecularColor;

            _effect.DirectionalLight2.Enabled = false;

            _effect.LightingEnabled = source.LightingEnabled;

            _effect.FogColor = source.FogColor;
            _effect.FogStart = source.FogStart;
            _effect.FogEnd = source.FogEnd;
            _effect.FogEnabled = source.FogEnabled;
        }
    }

    /// <summary>
    /// Helper to create procedural meshes.
    /// </summary>
    public static class ProceduralModels
    {
        private static void AddFace(List<VertexPositionNormalTexture> vertices, List<short> indices,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            short baseIndex = (short)vertices.Count;
            vertices.Add(new VertexPositionNormalTexture(p0, normal, new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(p1, normal, new Vector2(1, 0)));
            vertices.Add(new VertexPositionNormalTexture(p2, normal, new Vector2(1, 1)));
            vertices.Add(new VertexPositionNormalTexture(p3, normal, new Vector2(0, 1)));
            indices.Add(baseIndex); indices.Add((short)(baseIndex + 1)); indices.Add((short)(baseIndex + 2));
            indices.Add(baseIndex); indices.Add((short)(baseIndex + 2)); indices.Add((short)(baseIndex + 3));
        }

        private static void AddBox(List<VertexPositionNormalTexture> vertices, List<short> indices,
            float cx, float cy, float cz, float sx, float sy, float sz)
        {
            float hx = sx / 2f, hy = sy / 2f, hz = sz / 2f;
            Vector3 o = new Vector3(cx, cy, cz);
            // Front (+Z)
            AddFace(vertices, indices, o + new Vector3(-hx, -hy, hz), o + new Vector3(hx, -hy, hz), o + new Vector3(hx, hy, hz), o + new Vector3(-hx, hy, hz), Vector3.Backward);
            // Back (-Z)
            AddFace(vertices, indices, o + new Vector3(hx, -hy, -hz), o + new Vector3(-hx, -hy, -hz), o + new Vector3(-hx, hy, -hz), o + new Vector3(hx, hy, -hz), Vector3.Forward);
            // Top (+Y)
            AddFace(vertices, indices, o + new Vector3(-hx, hy, hz), o + new Vector3(hx, hy, hz), o + new Vector3(hx, hy, -hz), o + new Vector3(-hx, hy, -hz), Vector3.Up);
            // Bottom (-Y)
            AddFace(vertices, indices, o + new Vector3(-hx, -hy, -hz), o + new Vector3(hx, -hy, -hz), o + new Vector3(hx, -hy, hz), o + new Vector3(-hx, -hy, hz), Vector3.Down);
            // Right (+X)
            AddFace(vertices, indices, o + new Vector3(hx, -hy, hz), o + new Vector3(hx, -hy, -hz), o + new Vector3(hx, hy, -hz), o + new Vector3(hx, hy, hz), Vector3.Right);
            // Left (-X)
            AddFace(vertices, indices, o + new Vector3(-hx, -hy, -hz), o + new Vector3(-hx, -hy, hz), o + new Vector3(-hx, hy, hz), o + new Vector3(-hx, hy, -hz), Vector3.Left);
        }

        public static SimpleMesh CreateBox(GraphicsDevice gd, float sizeX = 1f, float sizeY = 1f, float sizeZ = 1f)
        {
            var vertices = new List<VertexPositionNormalTexture>();
            var indices = new List<short>();
            AddBox(vertices, indices, 0, 0, 0, sizeX, sizeY, sizeZ);
            return new SimpleMesh(gd, vertices.ToArray(), indices.ToArray());
        }

        public static SimpleMesh CreateTank(GraphicsDevice gd)
        {
            var vertices = new List<VertexPositionNormalTexture>();
            var indices = new List<short>();
            // Body: wide and flat
            AddBox(vertices, indices, 0, 0, 0, 3f, 1f, 5f);
            // Turret: smaller box on top
            AddBox(vertices, indices, 0, 1f, -0.5f, 1.5f, 0.8f, 2f);
            // Barrel: long thin box extending forward from turret
            AddBox(vertices, indices, 0, 1.2f, 2f, 0.3f, 0.3f, 3f);
            return new SimpleMesh(gd, vertices.ToArray(), indices.ToArray());
        }
    }

    public class QuadtreeGameComponent : DrawableGameComponent
    {
        public QuadtreeGameComponent(Game game)
            : base(game)
        {
            _quadtree = null;
            qNode = null;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        public virtual void Draw(GameTime gameTime, Matrix view, Matrix projection)
        {
            Draw(gameTime);
        }

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                if (qNode != null)
                {
                    if (qNode.IsInThisNode(Position))
                        return;
                    qNode.RemoveComponent(this);
                }
                if (_quadtree != null)
                    _quadtree.AddComponent(this);
            }
        }

        public Quadtree Quadtree
        {
            get { return _quadtree; }
        }

        internal Quadtree _quadtree;
        internal QuadTreeNode qNode;
        internal Vector3 _position;
    }

    //=======================================
    //  QuadTreeModelInfo
    //=======================================

    public class QuadtreeModelInfo
    {
        internal QuadtreeModelInfo(SimpleMesh m, Vector3 p, float s, Vector3 r)
        {
            mesh = m;
            position = p;
            rotation = r;
            scale = s;
            node = null;
            userObject = null;
        }

        public Vector3 Position
        {
            get { return position; }
        }
        public SimpleMesh Mesh
        {
            get { return mesh; }
        }
        public float Scale
        {
            get { return scale; }
        }
        public Vector3 Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        internal SimpleMesh mesh;
        internal Vector3 position;
        internal float scale;
        internal Vector3 rotation;
        internal QuadTreeNode node;
        public Object userObject;
    }

    //=======================================
    //  QuadTreeNode
    //=======================================

    class QuadTreeNode
    {
        const int NW = 0, NE = 1, SW = 2, SE = 3;

        public QuadTreeNode(QuadTreeNode parent)
        {
            m_parent = parent;
            child = new QuadTreeNode[4];
            m_corners = new Vector3[8];
            m_projCorners = new Vector4[8];
            models = new Collection<QuadtreeModelInfo>();
            gameComponents = new Collection<QuadtreeGameComponent>();
        }

        public bool Buildout(GraphicsDevice gd, float cellSize, int sectorsize, int minX, int minY, int width, int height, float[,] heightData, Vector3[,] normals)
        {
            m_width = width;
            m_height = height;

            if (minX + m_width >= heightData.GetLength(1))
                m_width = heightData.GetLength(1) - minX - 1;
            if (minY + m_height >= heightData.GetLength(0))
                m_height = heightData.GetLength(0) - minY - 1;

            m_cellSize = cellSize;

            m_startX = cellSize * (float)minX;
            m_startY = cellSize * (float)minY;
            m_endX = cellSize * (float)(minX + m_width);
            m_endY = cellSize * (float)(minY + m_height);

            if (m_width > sectorsize || m_height > sectorsize)
            {
                int halfWidth = m_width / 2;
                if ((m_width & 0x01) == 0x01)
                    halfWidth++;
                int halfHeight = m_height / 2;
                if ((halfHeight & 0x01) == 0x01)
                    halfHeight++;

                m_minHeight = 999.0f;
                m_maxHeight = -999.0f;

                if (child[NW] == null)
                    child[NW] = new QuadTreeNode(this);
                child[NW].Buildout(gd, cellSize, sectorsize, minX, minY, halfWidth, halfHeight, heightData, normals);
                m_minHeight = child[NW].m_minHeight < m_minHeight ? child[NW].m_minHeight : m_minHeight;
                m_maxHeight = child[NW].m_maxHeight > m_maxHeight ? child[NW].m_maxHeight : m_maxHeight;

                if (width > sectorsize)
                {
                    if (child[NE] == null)
                        child[NE] = new QuadTreeNode(this);
                    child[NE].Buildout(gd, cellSize, sectorsize, minX + halfWidth, minY, halfWidth, halfHeight, heightData, normals);
                    m_minHeight = child[NE].m_minHeight < m_minHeight ? child[NE].m_minHeight : m_minHeight;
                    m_maxHeight = child[NE].m_maxHeight > m_maxHeight ? child[NE].m_maxHeight : m_maxHeight;
                }
                if (height > sectorsize)
                {
                    if (child[SW] == null)
                        child[SW] = new QuadTreeNode(this);
                    child[SW].Buildout(gd, cellSize, sectorsize, minX, minY + halfHeight, halfWidth, halfHeight, heightData, normals);
                    m_minHeight = child[SW].m_minHeight < m_minHeight ? child[SW].m_minHeight : m_minHeight;
                    m_maxHeight = child[SW].m_maxHeight > m_maxHeight ? child[SW].m_maxHeight : m_maxHeight;
                }

                if (height > sectorsize && width > sectorsize)
                {
                    if (child[SE] == null)
                        child[SE] = new QuadTreeNode(this);
                    child[SE].Buildout(gd, cellSize, sectorsize, minX + halfWidth, minY + halfHeight, halfWidth, halfHeight, heightData, normals);
                    m_minHeight = child[SE].m_minHeight < m_minHeight ? child[SE].m_minHeight : m_minHeight;
                    m_maxHeight = child[SE].m_maxHeight > m_maxHeight ? child[SE].m_maxHeight : m_maxHeight;
                }

                ComputeCorners();
                return true;
            }

            m_heightData = new float[m_height + 1, m_width + 1];

            m_minHeight = 9999.0f;
            m_maxHeight = -9999.0f;

            for (int y = 0; y <= m_height; y++)
            {
                for (int x = 0; x <= m_width; x++)
                {
                    m_heightData[y, x] = heightData[minY + y, minX + x];
                    float data = m_heightData[y, x];
                    if (data > m_maxHeight) m_maxHeight = data;
                    if (data < m_minHeight) m_minHeight = data;
                }
            }

            ComputeCorners();

            float totalHeight = (float)heightData.GetLength(0);
            float totalWidth = (float)heightData.GetLength(1);

            float ustep = 1.0f / totalWidth;
            float ustart = (float)minX * ustep;
            float vstep = 1.0f / totalHeight;
            float vstart = (float)minY * vstep;

            return CreateMeshFromHeightData(gd, minX, minY, ustart, ustep, vstart, vstep, normals);
        }

        protected bool CreateMeshFromHeightData(GraphicsDevice gd, int minX, int minY, float ustart, float ustep, float vstart, float vstep, Vector3[,] normals)
        {
            m_numVerts = (m_width + 1) * (m_height + 1);

            m_vertexBuffer = new VertexBuffer(gd, typeof(VertexPositionNormalTexture), m_numVerts, BufferUsage.None);

            VertexPositionNormalTexture[] vertList = new VertexPositionNormalTexture[m_numVerts];

            int i = 0;
            for (int y = 0; y <= m_height; y++)
            {
                for (int x = 0; x <= m_width; x++)
                {
                    vertList[i].Position = new Vector3(m_startX + (x * m_cellSize), m_heightData[y, x], m_startY + (y * m_cellSize));
                    vertList[i].Normal = normals[minY + y, minX + x];
                    vertList[i].TextureCoordinate = new Vector2(ustart + (x * ustep), vstart + (y * vstep));
                    i++;
                }
            }

            m_vertexBuffer.SetData<VertexPositionNormalTexture>(vertList);

            m_numTris = m_width * m_height * 2;
            short[] indices = new short[m_numTris * 3];

            int vertsPerRow = m_width + 1;
            i = 0;
            for (int y = 0; y < m_height; y++)
            {
                int vertStart = y * vertsPerRow;
                for (int x = 0; x < m_width; x++)
                {
                    indices[i++] = (short)(vertStart + vertsPerRow);
                    indices[i++] = (short)vertStart;
                    indices[i++] = (short)(vertStart + 1);
                    indices[i++] = (short)(vertStart + vertsPerRow);
                    indices[i++] = (short)(vertStart + 1);
                    indices[i++] = (short)(vertStart + vertsPerRow + 1);
                    vertStart++;
                }
            }

            m_indexBuffer = new IndexBuffer(gd, IndexElementSize.SixteenBits, indices.Length, BufferUsage.None);
            m_indexBuffer.SetData<short>(indices);

            return true;
        }

        void ComputeCorners()
        {
            m_corners[0].X = m_corners[1].X = m_startX;
            m_corners[2].X = m_corners[3].X = m_endX;
            m_corners[0].Z = m_corners[3].Z = m_startY;
            m_corners[1].Z = m_corners[2].Z = m_endY;
            m_corners[0].Y = m_corners[1].Y = m_corners[2].Y = m_corners[3].Y = m_minHeight;

            m_corners[4].X = m_corners[5].X = m_startX;
            m_corners[6].X = m_corners[7].X = m_endX;
            m_corners[4].Z = m_corners[7].Z = m_startY;
            m_corners[5].Z = m_corners[6].Z = m_endY;
            m_corners[4].Y = m_corners[5].Y = m_corners[6].Y = m_corners[7].Y = m_maxHeight;
        }

        int TryCull(Matrix viewproj)
        {
            int t1, t2, t3, t4;
            t1 = t2 = t3 = t4 = 0;

            int i;
            for (i = 0; i < 8; i++)
                m_projCorners[i] = Vector4.Transform(m_corners[i], viewproj);

            for (i = 0; i < 8; i++)
                if (m_projCorners[i].Z < 0.0f) t1++;
            if (t1 >= 8) return 0;

            for (i = 0; i < 8; i++)
                if (m_projCorners[i].X > m_projCorners[i].W) t2++;
            if (t2 >= 8) return 0;

            for (i = 0; i < 8; i++)
                if (m_projCorners[i].X < -m_projCorners[i].W) t3++;
            if (t3 >= 8) return 0;

            for (i = 0; i < 8; i++)
                if (m_projCorners[i].Z > m_projCorners[i].W) t4++;
            if (t4 >= 8) return 0;

            if ((t1 + t2 + t3 + t4) == 0) return 1;
            return -1;
        }

        public void Draw(GraphicsDevice gd, Matrix viewproj, bool bCheckCull)
        {
            nodeDrawn = false;

            int cull = 1;
            if (bCheckCull)
            {
                cull = TryCull(viewproj);
                if (cull == 0) return;
            }

            nodeDrawn = true;

            if (m_vertexBuffer != null)
            {
                gd.SetVertexBuffer(m_vertexBuffer);
                gd.Indices = m_indexBuffer;
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, m_numTris);
            }
            for (int i = 0; i < 4; i++)
            {
                if (child[i] != null)
                    child[i].Draw(gd, viewproj, cull < 0);
            }
        }

        public void DrawModels(Matrix view, Matrix proj)
        {
            if (!nodeDrawn) return;

            for (int i = 0; i < 4; i++)
            {
                if (child[i] != null)
                    child[i].DrawModels(view, proj);
            }

            if (models == null || models.Count < 1) return;

            foreach (QuadtreeModelInfo minfo in models)
            {
                Matrix world = Matrix.CreateScale(minfo.scale)
                    * Matrix.CreateRotationX(minfo.rotation.X)
                    * Matrix.CreateRotationZ(minfo.rotation.Z)
                    * Matrix.CreateRotationY(minfo.rotation.Y)
                    * Matrix.CreateTranslation(minfo.position);
                minfo.mesh.Draw(world, view, proj);
            }
        }

        internal bool AddModel(QuadtreeModelInfo modelInfo)
        {
            if (models == null) return false;

            if (modelInfo.position.X >= m_startX && modelInfo.position.X < m_endX)
            {
                if (modelInfo.position.Z >= m_startY && modelInfo.position.Z < m_endY)
                {
                    if (child != null)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (child[i] != null)
                            {
                                if (child[i].AddModel(modelInfo))
                                    return true;
                            }
                        }
                    }
                    if (modelInfo.position.Y == 0)
                        modelInfo.position.Y = GetHeightAt(modelInfo.position.X, modelInfo.position.Z);
                    modelInfo.node = this;
                    models.Add(modelInfo);
                    return true;
                }
            }
            return false;
        }

        internal bool RemoveModel(QuadtreeModelInfo modelInfo)
        {
            if (models.Contains(modelInfo))
            {
                models.Remove(modelInfo);
                return true;
            }
            return false;
        }

        public void DrawComponents(GameTime gameTime, Matrix view, Matrix proj)
        {
            if (!nodeDrawn) return;

            if (gameComponents != null)
            {
                foreach (QuadtreeGameComponent component in gameComponents)
                    component.Draw(gameTime, view, proj);
            }

            if (child != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (child[i] != null)
                        child[i].DrawComponents(gameTime, view, proj);
                }
            }
        }

        internal bool AddComponent(QuadtreeGameComponent component)
        {
            if (gameComponents == null) return false;
            if (!IsInThisNode(component.Position)) return false;

            if (child != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (child[i] != null)
                    {
                        if (child[i].AddComponent(component))
                            return true;
                    }
                }
            }
            if (component._position.Y == 0)
                component._position.Y = GetHeightAt(component.Position.X, component.Position.Z);
            component.qNode = this;
            gameComponents.Add(component);
            return true;
        }

        internal bool RemoveComponent(QuadtreeGameComponent component)
        {
            if (gameComponents.Contains(component))
            {
                gameComponents.Remove(component);
                component.qNode = null;
                return true;
            }
            return false;
        }

        internal bool IsInThisNode(float x, float z)
        {
            return (x >= m_startX && x < m_endX && z >= m_startY && z < m_endY);
        }

        internal bool IsInThisNode(Vector3 position)
        {
            return IsInThisNode(position.X, position.Z);
        }

        private static float OutOfRange = -987654321.0f;

        public float GetHeightAt(float x, float y)
        {
            if (x < m_startX || x >= m_endX) return OutOfRange;
            if (y < m_startY || y >= m_endY) return OutOfRange;

            if (m_heightData == null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (child[i] != null)
                    {
                        float ret = child[i].GetHeightAt(x, y);
                        if (ret != OutOfRange) return ret;
                    }
                }
                return OutOfRange;
            }

            float thisX = x - m_startX;
            float thisY = y - m_startY;
            int mapX = (int)(thisX / m_cellSize);
            int mapY = (int)(thisY / m_cellSize);
            if (mapX < 0 || mapX >= m_width) return OutOfRange;
            if (mapY < 0 || mapY >= m_height) return OutOfRange;

            float deltaX = (thisX % m_cellSize) / m_cellSize;
            float deltaY = (thisY % m_cellSize) / m_cellSize;

            float hx1, hx2, hy1, hy2;
            if (deltaY < deltaX)
            {
                hx1 = m_heightData[mapY, mapX];
                hx2 = m_heightData[mapY, mapX + 1];
                hy1 = m_heightData[mapY, mapX];
                hy2 = m_heightData[mapY + 1, mapX];
            }
            else
            {
                hx1 = m_heightData[mapY + 1, mapX];
                hx2 = m_heightData[mapY + 1, mapX + 1];
                hy1 = m_heightData[mapY, mapX + 1];
                hy2 = m_heightData[mapY + 1, mapX + 1];
            }

            return ((hx1 + ((hx2 - hx1) * deltaX)) + (hy1 + ((hy2 - hy1) * deltaY))) / 2.0f;
        }

        public Collection<NodeDrawn> GetNodesDrawn(int level)
        {
            if (_nodesDrawn == null)
                _nodesDrawn = new Collection<NodeDrawn>();
            if (level == 0) _nodesDrawn.Clear();

            if (nodeDrawn)
            {
                if (level == 0 || m_vertexBuffer != null)
                    _nodesDrawn.Add(new NodeDrawn(level, m_startX, m_startY, m_endX, m_endY));
                if (child != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (child[i] != null)
                            child[i].GetNodesDrawn(level + 1);
                    }
                }
            }
            return _nodesDrawn;
        }

        protected float m_startX, m_startY, m_endX, m_endY;
        protected float m_minHeight, m_maxHeight;
        protected Vector3[] m_corners;
        protected Vector4[] m_projCorners;
        protected int m_width, m_height;
        protected float m_cellSize;
        protected float[,] m_heightData;
        protected bool nodeDrawn;
        protected QuadTreeNode[] child;
        protected int m_numVerts;
        protected VertexBuffer m_vertexBuffer;
        protected int m_numTris;
        protected IndexBuffer m_indexBuffer;
        protected QuadTreeNode m_parent;
        private Collection<QuadtreeModelInfo> models;
        private Collection<QuadtreeGameComponent> gameComponents;
        static internal Collection<NodeDrawn> _nodesDrawn;
    }

    public class NodeDrawn
    {
        internal NodeDrawn(int lev, float lX, float tZ, float rX, float bZ)
        {
            level = lev;
            upperLeft.X = lX;
            upperLeft.Y = tZ;
            lowerRight.X = rX;
            lowerRight.Y = bZ;
        }
        public int level;
        public Vector2 upperLeft;
        public Vector2 lowerRight;
    }

    //============================================================
    //  Quadtree
    //============================================================

    public class Quadtree : DrawableGameComponent
    {
        private bool m_valid;
        private QuadTreeNode m_root;
        private int m_sectorsize;
        private float totalWidth;
        private float totalHeight;
        private BasicEffect m_basicEffect;
        private Matrix m_view;
        private Matrix m_projection;
        private Matrix m_viewproj;
        private Collection<QuadtreeGameComponent> gameComponents;
        private RasterizerState _rasterizerState;

        public Quadtree(Game game) : base(game)
        {
            m_valid = false;
            m_root = null;
            gameComponents = new Collection<QuadtreeGameComponent>();
        }

        public bool Valid { get { return m_valid; } }
        public Matrix View { set { m_view = value; } }
        public Matrix Projection { set { m_projection = value; } }

        public Texture2D Texture
        {
            get { return m_basicEffect.Texture; }
            set
            {
                m_basicEffect.Texture = value;
                m_basicEffect.TextureEnabled = (m_basicEffect.Texture != null);
            }
        }

        public Vector3 AmbientLight
        {
            get { return m_basicEffect.AmbientLightColor; }
            set { m_basicEffect.AmbientLightColor = value; }
        }

        public bool FogEnabled
        {
            get { return m_basicEffect.FogEnabled; }
            set { m_basicEffect.FogEnabled = value; }
        }

        public Vector3 FogColor
        {
            get { return m_basicEffect.FogColor; }
            set { m_basicEffect.FogColor = value; }
        }

        public float FogStart
        {
            get { return m_basicEffect.FogStart; }
            set { m_basicEffect.FogStart = value; }
        }

        public float FogEnd
        {
            get { return m_basicEffect.FogEnd; }
            set { m_basicEffect.FogEnd = value; }
        }

        public BasicEffect Effect { get { return m_basicEffect; } }

        public float Width { get { return totalWidth; } }
        public float Height { get { return totalHeight; } }

        public override void Initialize()
        {
            base.Initialize();
            CreateEffect();

            _rasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullCounterClockwiseFace
            };

            foreach (QuadtreeGameComponent component in gameComponents)
                component.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            foreach (QuadtreeGameComponent component in gameComponents)
                component.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (m_basicEffect == null)
                CreateEffect();

            if (m_valid && m_root != null)
            {
                m_viewproj = m_view * m_projection;

                m_basicEffect.Projection = m_projection;
                m_basicEffect.View = m_view;
                m_basicEffect.World = Matrix.Identity;

                GraphicsDevice.RasterizerState = _rasterizerState;

                foreach (EffectPass pass in m_basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    m_root.Draw(this.GraphicsDevice, m_viewproj, true);
                }

                m_root.DrawModels(m_view, m_projection);
                m_root.DrawComponents(gameTime, m_view, m_projection);
            }
        }

        public bool Create(Texture2D heightMap, float cellSize, int sectorSize)
        {
            Int32[] data = new Int32[heightMap.Width * heightMap.Height];
            heightMap.GetData<Int32>(data);
            float[,] floatHeightData = ConvertARGBtoFloatHeightData(heightMap.Width, heightMap.Height, 2.5f, data);
            data = null;
            return Create(cellSize, sectorSize, floatHeightData);
        }

        public bool Create(float cellSize, int sectorSize, float[,] heightData)
        {
            int width = heightData.GetLength(1);
            int height = heightData.GetLength(0);

            totalWidth = cellSize * width;
            totalHeight = cellSize * height;
            m_sectorsize = sectorSize;

            Vector3[,] normals = CreateNormals(cellSize, heightData);

            m_root = new QuadTreeNode(null);
            m_valid = m_root.Buildout(this.GraphicsDevice, cellSize, sectorSize, 0, 0, width, height, heightData, normals);
            if (!m_valid) m_root = null;
            normals = null;

            if (m_valid && m_root != null)
            {
                foreach (QuadtreeGameComponent component in gameComponents)
                    m_root.AddComponent(component);
            }

            return m_valid;
        }

        public float GetHeightAt(float x, float z)
        {
            return m_root.GetHeightAt(x, z);
        }

        public Collection<NodeDrawn> GetNodesDrawn()
        {
            if (m_valid && m_root != null)
                return m_root.GetNodesDrawn(0);
            return null;
        }

        public QuadtreeModelInfo AddModel(SimpleMesh mesh, Vector3 position, float scale, Vector3 rotation)
        {
            if (m_valid && m_root != null)
            {
                QuadtreeModelInfo modelInfo = new QuadtreeModelInfo(mesh, position, scale, rotation);
                if (m_root.AddModel(modelInfo))
                    return modelInfo;
            }
            return null;
        }

        public void UpdateModelPosition(QuadtreeModelInfo modelInfo, Vector3 position)
        {
            if (m_valid && m_root != null && modelInfo != null)
            {
                if (modelInfo.node != null)
                    modelInfo.node.RemoveModel(modelInfo);
                modelInfo.position = position;
                m_root.AddModel(modelInfo);
            }
        }

        public void RemoveModel(QuadtreeModelInfo modelInfo)
        {
            if (modelInfo != null && modelInfo.node != null)
                modelInfo.node.RemoveModel(modelInfo);
        }

        public bool AddComponent(QuadtreeGameComponent component)
        {
            if (!gameComponents.Contains(component))
                gameComponents.Add(component);

            if (component.qNode != null && component._quadtree == this)
                component.qNode.RemoveComponent(component);
            component.qNode = null;
            component._quadtree = this;

            if (m_valid && m_root != null)
                m_root.AddComponent(component);

            return true;
        }

        public void RemoveComponent(QuadtreeGameComponent component)
        {
            if (component != null)
            {
                if (component._quadtree != this) return;
                if (component.qNode != null)
                    component.qNode.RemoveComponent(component);
                component.qNode = null;
                if (gameComponents.Contains(component))
                    gameComponents.Remove(component);
                component._quadtree = null;
            }
        }

        private Vector3[,] CreateNormals(float cellSize, float[,] heightData)
        {
            int height = heightData.GetLength(0);
            int width = heightData.GetLength(1);
            Vector3[,] normals = new Vector3[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || y == 0 || x == (width - 1) || y == (height - 1))
                    {
                        normals[y, x] = Vector3.Up;
                    }
                    else
                    {
                        Vector3 pos = new Vector3(cellSize * x, heightData[y, x], cellSize * y);
                        Vector3 pos2, pos3;

                        pos2 = new Vector3(cellSize * x, heightData[y - 1, x], cellSize * (y - 1));
                        pos3 = new Vector3(cellSize * (x - 1), heightData[y, x - 1], cellSize * y);
                        pos2 -= pos; pos3 -= pos; pos2.Normalize(); pos3.Normalize();
                        Vector3 norm1 = Vector3.Cross(pos2, pos3);

                        pos2 = new Vector3(cellSize * (x - 1), heightData[y, x - 1], cellSize * y);
                        pos3 = new Vector3(cellSize * (x - 1), heightData[y + 1, x - 1], cellSize * (y + 1));
                        pos2 -= pos; pos3 -= pos; pos2.Normalize(); pos3.Normalize();
                        Vector3 norm2 = Vector3.Cross(pos2, pos3);

                        pos2 = new Vector3(cellSize * (x - 1), heightData[y + 1, x - 1], cellSize * (y + 1));
                        pos3 = new Vector3(cellSize * x, heightData[y + 1, x], cellSize * (y + 1));
                        pos2 -= pos; pos3 -= pos; pos2.Normalize(); pos3.Normalize();
                        Vector3 norm3 = Vector3.Cross(pos2, pos3);

                        pos2 = new Vector3(cellSize * x, heightData[y + 1, x], cellSize * (y + 1));
                        pos3 = new Vector3(cellSize * (x + 1), heightData[y, x + 1], cellSize * y);
                        pos2 -= pos; pos3 -= pos; pos2.Normalize(); pos3.Normalize();
                        Vector3 norm4 = Vector3.Cross(pos2, pos3);

                        pos2 = new Vector3(cellSize * (x + 1), heightData[y, x + 1], cellSize * y);
                        pos3 = new Vector3(cellSize * (x + 1), heightData[y - 1, x + 1], cellSize * (y - 1));
                        pos2 -= pos; pos3 -= pos; pos2.Normalize(); pos3.Normalize();
                        Vector3 norm5 = Vector3.Cross(pos2, pos3);

                        pos2 = new Vector3(cellSize * (x + 1), heightData[y - 1, x + 1], cellSize * (y - 1));
                        pos3 = new Vector3(cellSize * x, heightData[y - 1, x], cellSize * (y - 1));
                        pos2 -= pos; pos3 -= pos; pos2.Normalize(); pos3.Normalize();
                        Vector3 norm6 = Vector3.Cross(pos2, pos3);

                        normals[y, x] = Vector3.Normalize((norm1 + norm2 + norm3 + norm4 + norm5 + norm6) / 6.0f);
                    }
                }
            }
            return normals;
        }

        protected float[,] ConvertARGBtoFloatHeightData(int width, int height, float yScale, Int32[] data)
        {
            int size = data.Length;
            if (size != width * height) return null;
            float[,] ret = new float[height, width];
            int r = 0, c = 0;
            for (int i = 0; i < size; i++)
            {
                ret[r, c] = (float)(data[i] & 0x000000FF) * yScale;
                c++;
                c %= width;
                if (c == 0) r++;
            }
            return ret;
        }

        protected void CreateEffect()
        {
            m_basicEffect = new BasicEffect(this.GraphicsDevice);
            m_basicEffect.Alpha = 1.0f;
            m_basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            m_basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            m_basicEffect.SpecularPower = 5.0f;
            m_basicEffect.AmbientLightColor = new Vector3(0.4f, 0.4f, 0.5f);

            m_basicEffect.DirectionalLight0.Enabled = true;
            m_basicEffect.DirectionalLight0.DiffuseColor = Vector3.One;
            m_basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.5f, -0.15f, -0.5f));
            m_basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

            m_basicEffect.DirectionalLight1.Enabled = false;
            m_basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
            m_basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
            m_basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

            m_basicEffect.LightingEnabled = true;

            m_basicEffect.FogColor = Color.CornflowerBlue.ToVector3();
            m_basicEffect.FogStart = 3000.0f;
            m_basicEffect.FogEnd = 4000.0f - 2.0f;
            m_basicEffect.FogEnabled = true;
        }
    }
}
