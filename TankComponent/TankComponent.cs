using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuadtreeComponent;

namespace TankGameComponentLib
{
    public class TankGameComponent : QuadtreeGameComponent
    {
        private int ID;
        private SimpleMesh myMesh;
        private float modelScale;
        private float modelRotation;
        private Vector3 velocity;
        private Vector3 acceleration;
        private static Random rand = new Random(1955);

        public TankGameComponent(Game game, int id)
            : base(game)
        {
            ID = id;
            modelRotation = 0.0f;
            modelScale = 3.0f;
            velocity = new Vector3(0.0f, 0.0f, 0.0f);
            acceleration = new Vector3(0.0f, 0.0f, 150.0f);
        }

        public override void Initialize()
        {
            modelRotation = (float)rand.NextDouble() * MathHelper.TwoPi;
            base.Initialize();
        }

        public void SetMesh(SimpleMesh mesh)
        {
            myMesh = mesh;
        }

        public SimpleMesh Mesh
        {
            get { return myMesh; }
        }

        public override void Update(GameTime gameTime)
        {
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector3 pos = this.Position;
            Matrix mat = Matrix.CreateRotationY(modelRotation);
            pos += Vector3.Transform(velocity, mat) * seconds;
            velocity += acceleration * seconds;
            if (velocity.Z > 140)
            {
                velocity.Z = 140;
            }
            float y = Quadtree.GetHeightAt(pos.X, pos.Z) + 10.0f;
            pos.Y = (y - pos.Y) * 0.1f + pos.Y;
            if (pos.X < 90.0f)
            {
                pos.X = Quadtree.Width - 100.0f;
            }
            if (pos.X > Quadtree.Width - 90.0f)
            {
                pos.X = 100.0f;
            }
            if (pos.Z < 90.0f)
            {
                pos.Z = Quadtree.Height - 100.0f;
            }
            if (pos.Z > Quadtree.Height - 90.0f)
            {
                pos.Z = 100.0f;
            }
            this.Position = pos;
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, Matrix view, Matrix projection)
        {
            if (myMesh == null) return;

            Matrix world = Matrix.CreateScale(modelScale)
                * Matrix.CreateRotationY(modelRotation + MathHelper.Pi)
                * Matrix.CreateTranslation(Position);

            myMesh.Draw(world, view, projection);

            base.Draw(gameTime, view, projection);
        }
    }
}
