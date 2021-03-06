﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using EntityEngineV4.CollisionEngine.Shapes;
using EntityEngineV4.Components;
using EntityEngineV4.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EntityEngineV4.CollisionEngine
{
    /// <summary>
    /// A state-side service for handling collisions and dealing with the resolution of those collisions.
    /// </summary>
    public class CollisionHandler : Service
    {
        //TODO: Fix AABBvsAABB collision bug

        /// <summary>
        /// List of colliding members on this state.
        /// </summary>
        private List<Collision> _collideables;

        /// <summary>
        /// Pairs to be sent in for testing.
        /// </summary>
        private HashSet<Pair> _pairs;

        /// <summary>
        /// The pairs that have already collided and generated a manifold as a result.
        /// </summary>
        private HashSet<Manifold> _manifolds;

        public int CollisionsThisFrame { get { return _manifolds.Count; } }

        public CollisionHandler(State stateref)
            : base(stateref, "CollisionHandler")
        {
            _collideables = new List<Collision>();
            _pairs = new HashSet<Pair>();
            _manifolds = new HashSet<Manifold>();
        }

        public override void Update(GameTime gt)
        {
            BroadPhase();
            foreach (var manifold in _manifolds)
            {
                manifold.A.OnCollision(manifold);
                manifold.B.OnCollision(manifold);

                if (CanObjectsResolve(manifold.A, manifold.B) || CanObjectsResolve(manifold.B, manifold.A))
                {
                    ResolveCollision(manifold);
                    PositionalCorrection(manifold);
                }
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            _manifolds.Clear();
        }

        public void AddCollision(Collision c)
        {
            //Check if the Collision is already in the list.
            if (Enumerable.Contains(_collideables, c)) return;
            _collideables.Add(c);

            //Generate our pairs
            ReconfigurePairs(c);
        }

        public void RemoveCollision(Collision c)
        {
            if (!Enumerable.Contains(_collideables, c)) return;
            _collideables.Remove(c);

            _pairs.RemoveWhere(pair => pair.A.Equals(c) || pair.B.Equals(c));
        }

        public IEnumerable<Collision> GetColliding()
        {
            var output = new HashSet<Collision>();

            foreach (var manifold in _manifolds)
            {
                output.Add(manifold.A);
                output.Add(manifold.B);
            }

            return output;
        }

        /// <summary>
        /// Reconfigures the pairs for a Collision c
        /// </summary>
        /// <param name="c">A collision.</param>
        public void ReconfigurePairs(Collision c)
        {
            //Remove pairs with this collision in it
            foreach (var pair in _pairs.ToArray().Where(pair => pair.A.Equals(c) || pair.B.Equals(c)))
            {
                _pairs.Remove(pair);
            }

            //Recalculate pairs with this new collision
            foreach (var other in _collideables)
            {
                if (c.Equals(other)) continue;
                if (CanObjectsPair(c, other))
                {
                    var p = new Pair(c, other);
                    _pairs.Add(p);
                }
            }
        }

        /// <summary>
        /// Generates the pairs used for testing collision.
        /// </summary>
        public void GeneratePairs()
        {
            if (_collideables.Count() <= 1) return;

            _pairs.Clear();

            foreach (var a in _collideables)
            {
                foreach (var b in _collideables)
                {
                    if (a.Equals(b)) continue;
                    if (CanObjectsPair(a, b))
                    {
                        var p = new Pair(a, b);
                        _pairs.Add(p);
                    }
                }
            }
        }

        public void BroadPhase()
        {
            //Do a basic SAT test
            //foreach (var pair in _pairs)
            //{
            //    Manifold m = AABBvsAABB(AABB.Create(pair.A), AABB.Create(pair.B));
            //    if (m.AreColliding)
            //    {
            //        //Do our real test now.
            //        if (pair.A.Shape is AABB && pair.B.Shape is AABB)
            //            //If the shapes are both AABB's, skip the check, we already have it
            //            _manifolds.Add(m);
            //        else
            //        {
            //            m = CheckCollision(pair.A.Shape, pair.B.Shape);
            //            if (m.AreColliding)
            //                _manifolds.Add(m);
            //        }
            //    }
            //}

            _manifolds = ReturnManifolds();
        }

        //Static methods

        /// <summary>
        /// Compares the masks and checks to see if they should be allowed to form a pair.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Whether or not the the two objects should be paired</returns>
        public static bool CanObjectsPair(Collision a, Collision b)
        {
            return (a.Group.HasMatchingBit(b.Pair) || //Compare the pair masks to the group masks.
                    a.Pair.HasMatchingBit(b.Group));
        }

        /// <summary>
        /// Compares masks and checks to see if they should be allowed to resolve.
        /// </summary>
        /// <param name="resolver">Object being resolved</param>
        /// <param name="other">Other object</param>
        /// <returns></returns>
        public static bool CanObjectsResolve(Collision resolver, Collision other)
        {
            return resolver.ResolutionGroup.HasMatchingBit(other.ResolutionGroup) //Compare the pair mask one sided.
                && !resolver.Immovable && resolver.AllowResolution;
        }

        /// <summary>
        /// Resolves a collision from the manifold speicifed. 
        /// See this document http://gamedevelopment.tutsplus.com/tutorials/create-custom-2d-physics-engine-aabb-circle-impulse-resolution--gamedev-6331
        /// </summary>
        /// <param name="m"></param>
        public static void ResolveCollision(Manifold m)
        {
            if (!m.AreColliding) return;

            Vector2 relVelocity = m.B.Velocity - m.A.Velocity;
            //Finds out if the objects are moving towards each other.
            //We only need to resolve collisions that are moving towards, not away.
            float velAlongNormal = Physics.DotProduct(relVelocity, m.Normal);
            if (velAlongNormal > 0)
                return;
            float e = Math.Min(m.A.Restitution, m.B.Restitution);

            float j = -(1 + e) * velAlongNormal;
            j /= m.A.InvertedMass + m.B.InvertedMass;

            Vector2 impulse = j * m.Normal;
            if (CanObjectsResolve(m.A, m.B))
                m.A.Velocity -= m.A.InvertedMass * impulse;
            if (CanObjectsResolve(m.B, m.A))
                m.B.Velocity += m.B.InvertedMass * impulse;
        }

        public const float SLOP = 0.05f;
        public const float PERCENT = 0.6f;

        /// <summary>
        /// Corrects position errors created by floating point miscalculations
        /// </summary>
        /// <param name="m"></param>
        public static void PositionalCorrection(Manifold m)
        {
            Vector2 correction = Math.Max(m.PenetrationDepth - SLOP, 0.0f) / (m.A.InvertedMass + m.B.InvertedMass) * PERCENT * m.Normal;
            if (CanObjectsResolve(m.A, m.B))
                m.A.Position -= m.A.InvertedMass * correction;
            if (CanObjectsResolve(m.B, m.A))
                m.B.Position += m.B.InvertedMass * correction;
        }

        /// <summary>
        /// Tests for collision between two axis aligned bounding boxes.
        /// </summary>
        /// <param name="a">An AABB</param>
        /// <param name="b">An AABB</param>
        /// <param name="manifold">A manifold to output to.</param>
        /// <returns>If the two AABBs are colliding</returns>
        public static bool AABBvsAABB(AABB a, AABB b, ref Manifold manifold)
        {
            manifold.Normal = a.Position - b.Position;

            //Check if colliding on X
            if (!(a.Right < b.Left || a.Left > b.Right))
            {
                //Check if colliding Y
                if (!(a.Bottom < b.Top || a.Top > b.Bottom))
                {
                    float xPen = ((a.Left < b.Left) ? a.Width : b.Width) - Math.Abs(manifold.Normal.X);
                    float yPen = ((a.Top < b.Top) ? a.Height : b.Height) - Math.Abs(manifold.Normal.Y);
                    Vector2 faceNormal;

                    if (xPen > yPen)
                    {
                        faceNormal = manifold.Normal.X < 0 ? -Vector2.UnitX : Vector2.UnitX;
                        manifold.PenetrationDepth = xPen;
                        manifold.Normal = Physics.GetNormal(a.Position, b.Position);
                        manifold.Normal.X *= faceNormal.X;
                    }
                    else
                    {
                        faceNormal = manifold.Normal.Y < 0 ? -Vector2.UnitY : Vector2.UnitY;

                        manifold.PenetrationDepth = yPen;
                        manifold.Normal = Physics.GetNormal(a.Position, b.Position);
                        manifold.Normal.Y *= faceNormal.Y;
                    }
                    manifold.AreColliding = true;
                }
            }

            return manifold.AreColliding;
        }

        /// <summary>
        /// Tests for collision between two circles
        /// </summary>
        /// <param name="a">A circle</param>
        /// <param name="b">A circle</param>
        /// <param name="manifold">A manifold to output to.</param>
        /// <returns>If the two circles are colliding or not.</returns>
        public static bool CircleVSCircle(Circle a, Circle b, ref Manifold manifold)
        {
            manifold.Normal = b.Position - a.Position;
            float r = a.Radius + b.Radius;
            r *= r;
            float l = manifold.Normal.LengthSquared();
            if (l > r)
            {
                //Set manifold for failure
                manifold.AreColliding = false;
                return false;
            }

            float d = manifold.Normal.Length();
            manifold.Normal.Normalize();
            if (Math.Abs(d) > float.Epsilon)
            {
                manifold.PenetrationDepth = a.Radius + b.Radius - d;
                manifold.AreColliding = true;
                return true;
            }
            else
            {
                //find which one is bigger
                float maxRadius = Math.Max(a.Radius, b.Radius);
                manifold.PenetrationDepth = maxRadius;
                manifold.AreColliding = true;
                return true;
            }
        }

        //Collision resolver methods

        /// <summary>
        /// Uses a table to test collision between various shapes
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Manifold CheckCollision(Collision a, Collision b)
        {
            Shape aShape, bShape;
            aShape = a.GetDependency<Shape>(Collision.DEPENDENCY_SHAPE);
            bShape = b.GetDependency<Shape>(Collision.DEPENDENCY_SHAPE);

            Manifold manifold = new Manifold(a, b);

            if (aShape is AABB && bShape is AABB)
                AABBvsAABB((AABB)aShape, (AABB)bShape, ref manifold);
            else if (aShape is Circle && bShape is Circle)
                CircleVSCircle((Circle)aShape, (Circle)bShape, ref manifold);
            else
                throw new Exception("No existing methods for this kind of collision!");

            return manifold;
        }

        /// <summary>
        /// Returns a list of colliding manifolds on all active pairs. The tests are run on this call.
        /// </summary>
        /// <returns>a list of colliding manifolds on all active pairs.</returns>
        public HashSet<Manifold> ReturnManifolds()
        {
            var answer = new HashSet<Manifold>();
            foreach (var pair in _pairs.Where(
                p => p.A.IsActive && p.B.IsActive && !p.A.Recycled && !p.B.Recycled
                    && !p.A.Exclusions.Contains(p.B) && !p.B.Exclusions.Contains(p.A)))
            //Only check pairs who are active, not recycled, and not excluded from collisions
            {
                Manifold m = CheckCollision(pair.A, pair.B);
                if (m.AreColliding)
                    answer.Add(m);
            }
            return answer;
        }

        /// <summary>
        /// returns a list of colliding manifolds where `collision` is a member of the pairing. The tests are run on this call.
        /// </summary>
        /// <param name="collision"></param>
        /// <returns>a list of colliding manifolds where `collision` is a member of the pairing.</returns>
        public HashSet<Manifold> ReturnManifolds(Collision collision)
        {
            var answer = new HashSet<Manifold>();
            foreach (var pair in _pairs.Where(
                p => p.A.IsActive && p.B.IsActive && !p.A.Recycled && !p.B.Recycled
                    && !p.A.Exclusions.Contains(p.B) && !p.B.Exclusions.Contains(p.A)
                    && (p.A == collision || p.B == collision)))
            {
                Manifold m = CheckCollision(pair.A, pair.B);
                if (m.AreColliding)
                    answer.Add(m);
            }
            return answer;
        }
    }
}