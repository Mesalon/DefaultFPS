using UnityEngine.Serialization;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;
using System;

namespace Fusion.Animations {


    [Serializable] public class BlendTree {
        public BlendTreeNode[] nodes;
    }
    
    public abstract class MultiBlendTreeState : AnimationState, IAnimationTimeProvider {
        [SerializeField] private float speed = 1.0f;
        [SerializeField] private BlendTree[] trees;
        [SerializeField] private bool isLooping;

        private AnimationMixerPlayable _mixer;
        private AnimationBlendTree _blendTree;
        private float _animationTime;
        private float _interpolatedAnimationTime;
        private bool _isCacheValid;
        private float _cachedTargetLength; 
        private Vector2 _cachedPosition;

        // PUBLIC METHODS

        public void SetAnimationTime(float animationTime) {
            _animationTime = animationTime;
        }

        public bool IsFinished(float normalizedTime = 1.0f) {
            return !(_animationTime < normalizedTime) && !isLooping && IsActive();
        }

        protected abstract Vector2 GetBlendPosition(bool interpolated);

        protected override void CreatePlayable() {
            int nodeCount = trees.Length;

            _mixer = AnimationMixerPlayable.Create(Controller.Graph, trees.Length);

            Vector2[] blendTreePositions = new Vector2[nodeCount];

            for (int i = 0; i < nodeCount; ++i) {
                BlendTreeNode node = trees[0].nodes[i];

                node.CreatePlayable(Controller.Graph);
                blendTreePositions[i] = node.Position;

                _mixer.ConnectInput(i, node.PlayableClip, 0);
            }

            _blendTree = new AnimationBlendTree(blendTreePositions);

            AddPlayable(_mixer, 0);
        }

        protected override void OnDespawned() {
            if (_mixer.IsValid() == true) {
                _mixer.Destroy();
            }

            for (int i = 0, count = trees.Length; i < count; ++i) {
                trees[0].nodes[i].DestroyPlayable();
            }
        }

        protected override void OnFixedUpdate() {
            Vector2 blendPosition = GetBlendPosition(false);
            _animationTime = SetPosition(blendPosition, _animationTime, Controller.DeltaTime);
        }

        protected override void OnInterpolate() {
            Vector2 blendPosition = GetBlendPosition(true);
            SetPosition(blendPosition, _interpolatedAnimationTime, 0.0f);
        }

        protected override void OnSetDefaults() {
            _animationTime = 0.0f;
        }

        // IAnimationTimeProvider INTERFACE

        float IAnimationTimeProvider.AnimationTime {
            get { return _animationTime; }
            set { _animationTime = value; }
        }

        float IAnimationTimeProvider.InterpolatedAnimationTime {
            get { return _interpolatedAnimationTime; }
            set { _interpolatedAnimationTime = value; }
        }

        // PRIVATE METHODS

        private float SetPosition(Vector2 position, float animationTime, float deltaTime) {
            deltaTime *= speed;

            float targetLength = 0.0f;

            if (_isCacheValid == true && AlmostEquals(position, _cachedPosition, 0.01f) == true) {
                targetLength = _cachedTargetLength;
            }
            else {
                _blendTree.CalculateWeights(position);

                float[] weights = _blendTree.Weights;

                for (int i = 0, count = trees.Length; i < count; ++i) {
                    float weight = weights[i];
                    if (weight > 0.0f) {
                        targetLength += trees[0].nodes[i].Length / trees[0].nodes[i].Speed * weight;
                    }

                    _mixer.SetInputWeight(i, weight);
                }

                _isCacheValid = true;
                _cachedPosition = position;
                _cachedTargetLength = targetLength;
            }

            if (targetLength >= 0.001f) {
                deltaTime /= targetLength;
            }

            animationTime += deltaTime;
            if (animationTime > 1.0f) {
                if (isLooping == true) {
                    animationTime %= 1.0f;
                }
                else {
                    animationTime = 1.0f;
                }
            }

            for (int i = 0, count = trees.Length; i < count; ++i) {
                if (_blendTree.Weights[i] > 0.0f) {
                    BlendTreeNode node = trees[0].nodes[i];
                    node.PlayableClip.SetTime(animationTime * node.Length);
                }
            }

            return animationTime;
        }

        private static bool AlmostEquals(Vector2 vectorA, Vector2 vectorB, float tolerance = 0.01f) {
            Vector2 difference = vectorA - vectorB;
            return difference.x < tolerance && difference.x > -tolerance && difference.y < tolerance && difference.y > -tolerance;
        }
    }
}