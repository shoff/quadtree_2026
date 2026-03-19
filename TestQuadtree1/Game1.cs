using System;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using QuadtreeComponent;
using TankGameComponentLib;

namespace TestQuadtree1
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        Quadtree quadtree;
        const float CELLSIZE = 30.0f;
        long frameNum;

        // variables for Drawing the NodesDrawn
        bool showNodesDrawn;
        bool keyNWasDown = false;
        Matrix ScreenViewMatrix;
        Matrix ScreenProjectionMatrix;
        BasicEffect nodesEffect;
        VertexBuffer nodesVertBuf;
        IndexBuffer nodesIndexBuf;
        const int MAXNODEVERTS = 768;
        static VertexPositionColor[] _nodeVerts = new VertexPositionColor[MAXNODEVERTS];
        const int MAXNODEINDICES = MAXNODEVERTS * 2;
        static short[] _nodeIndices = new short[MAXNODEINDICES];
        const int NUMROCKS = 500;
        const int NUMTANKS = 50;
        Collection<TankGameComponent> tanks;

        // Players (avatar) variables
        Vector3 avatarPosition = new Vector3(4676f, 0f, 7194f);
        Vector3 avatarHeadOffset = new Vector3(0, 10, 0);
        float avatarYaw = 4.958328f;

        const float LOOKAHEAD = 600.0f;
        Vector3 cameraReference = new Vector3(0, 0, LOOKAHEAD);
        float currentLookatY = 0.0f;
        static float viewAngle = MathHelper.PiOver4;
        static float nearClip = 5.0f;
        static float farClip = 4300.0f;
        static float terminalVelocity = (5280.0f * 124.0f) / 60.0f / 60.0f;
        float rotationSpeed = 1f / 60f;
        float forwardSpeed = 117.333f / 60f;
        Vector3 acceleration = new Vector3(0.0f, 0.0f, 0.0f);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            showNodesDrawn = true;
            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;
            graphics.PreferMultiSampling = false;
        }

        protected override void Initialize()
        {
            ScreenViewMatrix = Matrix.Identity;
            ScreenProjectionMatrix = Matrix.CreateOrthographicOffCenter(0, graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height, 0, 0.0f, 1.0f);

            quadtree = new Quadtree(this);
            Components.Add(quadtree);

            Random rand = new Random(1955);
            tanks = new Collection<TankGameComponent>();
            for (int i = 0; i < NUMTANKS; i++)
            {
                TankGameComponent tank = new TankGameComponent(this, 100 + i);

                Vector3 pos;
                pos.X = (float)rand.NextDouble() * (CELLSIZE * 256) + (CELLSIZE * 128);
                pos.Z = (float)rand.NextDouble() * (CELLSIZE * 256) + (CELLSIZE * 128);
                pos.Y = 0.0f;

                tank.Position = pos;
                quadtree.AddComponent(tank);
                tanks.Add(tank);
            }

            CreateNodesDrawnEffect();

            base.Initialize();
        }

        private Texture2D LoadTextureFromFile(string filename)
        {
            // Try multiple base paths to find the content
            string[] basePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory),
                Content.RootDirectory,
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".", Content.RootDirectory)
            };

            foreach (string basePath in basePaths)
            {
                string path = Path.Combine(basePath, filename);
                if (File.Exists(path))
                {
                    using (var stream = File.OpenRead(path))
                    {
                        return Texture2D.FromStream(graphics.GraphicsDevice, stream);
                    }
                }
            }

            throw new FileNotFoundException($"Could not find content file: {filename}");
        }

        protected override void LoadContent()
        {
            // Load heightmap from raw image file
            Texture2D heightMap = LoadTextureFromFile("Hm2.jpg");
            quadtree.Create(heightMap, CELLSIZE, 16);

            // Load terrain texture
            quadtree.Texture = LoadTextureFromFile("LandscapeTex02.jpg");

            nodesVertBuf = new VertexBuffer(graphics.GraphicsDevice,
                typeof(VertexPositionColor),
                MAXNODEVERTS,
                BufferUsage.WriteOnly);
            nodesIndexBuf = new IndexBuffer(graphics.GraphicsDevice,
                IndexElementSize.SixteenBits,
                MAXNODEINDICES,
                BufferUsage.WriteOnly);

            quadtree.Effect.FogEnd = farClip - 2.0f;
            quadtree.Effect.FogStart = ComputeFogStart(nearClip, farClip);
            quadtree.Effect.FogColor = Color.CornflowerBlue.ToVector3();
            quadtree.Effect.FogEnabled = true;

            // Create procedural rock (box) mesh
            SimpleMesh rock = ProceduralModels.CreateBox(graphics.GraphicsDevice, 1f, 1f, 1f);
            rock.CopyEffectFrom(quadtree.Effect);
            Random r = new Random(512);
            for (int i = 0; i < NUMROCKS; i++)
            {
                float x = (float)r.NextDouble() * (510f * 30.0f);
                float z = (float)r.NextDouble() * (510f * 30.0f);
                float scale = (float)r.NextDouble() * 10.0f + 1.0f;
                float rotV = (float)r.NextDouble() * 6.28f;
                Vector3 rot = new Vector3(rotV, 0.0f, scale);
                quadtree.AddModel(rock, new Vector3(x, 0.0f, z), scale, rot);
            }

            // Create procedural tank mesh and assign to all tanks
            SimpleMesh tankMesh = ProceduralModels.CreateTank(graphics.GraphicsDevice);
            tankMesh.CopyEffectFrom(quadtree.Effect);

            for (int i = 0; i < NUMTANKS; i++)
            {
                tanks[i].SetMesh(tankMesh);
            }
        }

        protected override void UnloadContent()
        {
            quadtree = null;
            nodesEffect = null;
            nodesIndexBuf = null;
            nodesVertBuf = null;
        }

        protected override void Update(GameTime gameTime)
        {
            frameNum++;
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            UpdateAvatarPosition(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            UpdateCameraFirstPerson();

            quadtree.View = viewMatrix;
            quadtree.Projection = projectionMatrix;

            if (showNodesDrawn)
            {
                ShowNodesDrawn();
            }

            base.Draw(gameTime);
        }

        void ShowNodesDrawn()
        {
            Collection<NodeDrawn> nodes = quadtree.GetNodesDrawn();
            if (nodes == null) return;

            int cnt = nodes.Count;
            int numVerts = cnt * 4;

            if (numVerts > 0 && quadtree.Width > 2)
            {
                if (numVerts > MAXNODEVERTS)
                    numVerts = MAXNODEVERTS;

                Color c;
                int i = 0;
                Vector3 pos = new Vector3();
                float scale = (1.0f / quadtree.Width) * 100.0f;
                int n = 0;
                for (i = 0; i <= numVerts - 4; i += 4)
                {
                    NodeDrawn nd = nodes[n];
                    n++;
                    c = Color.White;
                    pos.X = scale * nd.upperLeft.X + 10.0f;
                    pos.Y = scale * nd.upperLeft.Y + 10.0f;
                    pos.Z = 0.0f;
                    _nodeVerts[i] = new VertexPositionColor(pos, c);
                    pos.X = scale * nd.lowerRight.X + 10.0f;
                    pos.Y = scale * nd.upperLeft.Y + 10.0f;
                    pos.Z = 0.0f;
                    _nodeVerts[i + 1] = new VertexPositionColor(pos, c);
                    pos.X = scale * nd.lowerRight.X + 10.0f;
                    pos.Y = scale * nd.lowerRight.Y + 10.0f;
                    pos.Z = 0.0f;
                    _nodeVerts[i + 2] = new VertexPositionColor(pos, c);
                    pos.X = scale * nd.upperLeft.X + 10.0f;
                    pos.Y = scale * nd.lowerRight.Y + 10.0f;
                    pos.Z = 0.0f;
                    _nodeVerts[i + 3] = new VertexPositionColor(pos, c);
                }
                nodesVertBuf.SetData<VertexPositionColor>(_nodeVerts, 0, numVerts);

                int numLines = numVerts;
                int numIndices = numLines * 2;
                int v = 0;
                for (i = 0; i <= numIndices - 8; i += 8)
                {
                    _nodeIndices[i + 0] = (short)(v + 0); _nodeIndices[i + 1] = (short)(v + 1);
                    _nodeIndices[i + 2] = (short)(v + 1); _nodeIndices[i + 3] = (short)(v + 2);
                    _nodeIndices[i + 4] = (short)(v + 2); _nodeIndices[i + 5] = (short)(v + 3);
                    _nodeIndices[i + 6] = (short)(v + 3); _nodeIndices[i + 7] = (short)(v + 0);
                    v += 4;
                }
                nodesIndexBuf.SetData<short>(_nodeIndices, 0, numIndices);

                foreach (EffectPass pass in nodesEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphics.GraphicsDevice.SetVertexBuffer(nodesVertBuf);
                    graphics.GraphicsDevice.Indices = nodesIndexBuf;
                    graphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, numLines);
                }
            }
        }

        void CreateNodesDrawnEffect()
        {
            nodesEffect = new BasicEffect(graphics.GraphicsDevice);
            nodesEffect.View = ScreenViewMatrix;
            nodesEffect.Projection = ScreenProjectionMatrix;
            nodesEffect.World = Matrix.Identity;
            nodesEffect.VertexColorEnabled = true;
            nodesEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            nodesEffect.AmbientLightColor = new Vector3(1.0f, 1.0f, 1.0f);
            nodesEffect.LightingEnabled = true;
        }

        void UpdateAvatarPosition(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState currentState = GamePad.GetState(PlayerIndex.One);

            if (keyboardState.IsKeyDown(Keys.Escape))
                this.Exit();

            if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.Q) || (currentState.DPad.Left == ButtonState.Pressed))
                avatarYaw += rotationSpeed;

            if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.E) || (currentState.DPad.Right == ButtonState.Pressed))
                avatarYaw -= rotationSpeed;

            float speed = forwardSpeed;
            if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                speed = forwardSpeed * 2.0f;

            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W) || (currentState.DPad.Up == ButtonState.Pressed))
            {
                Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                Vector3 v = Vector3.Transform(new Vector3(0, 0, speed), forwardMovement);
                avatarPosition.Z += v.Z;
                avatarPosition.X += v.X;
            }

            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S) || (currentState.DPad.Down == ButtonState.Pressed))
            {
                Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                Vector3 v = Vector3.Transform(new Vector3(0, 0, -(speed * 0.5f)), forwardMovement);
                avatarPosition.Z += v.Z;
                avatarPosition.X += v.X;
            }

            if (keyboardState.IsKeyDown(Keys.Space))
            {
                Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                Vector3 v = Vector3.Transform(new Vector3(0, speed, 0), forwardMovement);
                avatarPosition.Y += v.Y;
            }

            if (keyboardState.IsKeyDown(Keys.A))
            {
                Vector3 v = Vector3.Transform(new Vector3(speed * 0.75f, 0, 0), Matrix.CreateRotationY(avatarYaw));
                avatarPosition.Z += v.Z;
                avatarPosition.X += v.X;
            }

            if (keyboardState.IsKeyDown(Keys.D))
            {
                Vector3 v = Vector3.Transform(new Vector3(-speed * 0.75f, 0, 0), Matrix.CreateRotationY(avatarYaw));
                avatarPosition.Z += v.Z;
                avatarPosition.X += v.X;
            }

            float y = quadtree.GetHeightAt(avatarPosition.X, avatarPosition.Z);
            if (y < 0.0f) y = 0.0f;

            y = (y - avatarPosition.Y) * 0.707f + avatarPosition.Y;

            if (avatarPosition.Y > y)
            {
                float seconds = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0);
                acceleration.Y -= 32.1522f * seconds;
                if (acceleration.Y < -terminalVelocity)
                    acceleration.Y = -terminalVelocity;
                y = avatarPosition.Y + (acceleration.Y * seconds);
            }
            else
            {
                acceleration.Y = 0.0f;
            }

            avatarPosition.Y = y;

            float minDist = LOOKAHEAD * 2.0f;
            if (avatarPosition.X < minDist) avatarPosition.X = minDist;
            if (avatarPosition.Z < minDist) avatarPosition.Z = minDist;
            if (avatarPosition.X > quadtree.Width - minDist) avatarPosition.X = quadtree.Width - minDist;
            if (avatarPosition.Z > quadtree.Height - minDist) avatarPosition.Z = quadtree.Height - minDist;

            if (keyboardState.IsKeyDown(Keys.N))
            {
                if (!keyNWasDown)
                {
                    showNodesDrawn = !showNodesDrawn;
                    keyNWasDown = true;
                }
            }
            else
            {
                keyNWasDown = false;
            }
        }

        void UpdateCameraFirstPerson()
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(avatarYaw);
            Vector3 headOffset = Vector3.Transform(avatarHeadOffset, rotationMatrix);
            Vector3 cameraPosition = avatarPosition + headOffset;
            Vector3 transformedReference = Vector3.Transform(cameraReference, rotationMatrix);
            Vector3 cameraLookat = transformedReference + cameraPosition;
            float lookatY = quadtree.GetHeightAt(cameraLookat.X, cameraLookat.Z) + avatarHeadOffset.Y;
            cameraLookat.Y = currentLookatY + (lookatY - currentLookatY) * 0.06f;
            currentLookatY = cameraLookat.Y;

            viewMatrix = Matrix.CreateLookAt(cameraPosition, cameraLookat, new Vector3(0.0f, 1.0f, 0.0f));
            Viewport viewport = graphics.GraphicsDevice.Viewport;
            float aspectRatio = (float)viewport.Width / (float)viewport.Height;
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(viewAngle, aspectRatio, nearClip, farClip);
        }

        float ComputeFogStart(float nearClip, float farClip)
        {
            return (farClip - nearClip) * 0.6f + nearClip;
        }
    }
}
