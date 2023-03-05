using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace XNAPerformanceChecker
{
    public class CheckPerformance
    {
        private Vector3 cameraReference = new Vector3(0, 0, -1.0f);
        private Vector3 cameraPosition = new Vector3(0, 0, 3.0f);
        private Vector3 cameraTarget = Vector3.Zero;
        private Vector3 vectorUp = Vector3.Up;
        private Matrix projection;
        private Matrix view;
        private float cameraYaw = 0.0f;

        public CheckPerformance() { }

        public void TransformVectorByValue()
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(
                MathHelper.ToRadians(45.0f));
            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference = Vector3.Transform(cameraReference,
                rotationMatrix);
            // Calculate the position the camera is looking at.
            cameraTarget = cameraPosition + transformedReference;
        }

        public void TransformVectorByReference()
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(
                MathHelper.ToRadians(45.0f));
            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference;
            Vector3.Transform(ref cameraReference, ref rotationMatrix,
                out transformedReference);
            // Calculate the position the camera is looking at.
            Vector3.Add(ref cameraPosition, ref transformedReference,
                out cameraTarget);
        }

        public void TransformVectorByReferenceAndOut()
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(
                MathHelper.ToRadians(45.0f));
            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference;
            Vector3.Transform(ref cameraReference, ref rotationMatrix,
                out transformedReference);
            // Calculate the position the camera is looking at.
            Vector3.Add(ref cameraPosition, ref transformedReference,
                out cameraTarget);
        }

        public void TransformVectorByReferenceAndOutVectorAdd()
        {
            Matrix rotationMatrix;
            Matrix.CreateRotationY(MathHelper.ToRadians(45.0f),
                out rotationMatrix);
            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference;
            Vector3.Transform(ref cameraReference, ref rotationMatrix,
                out transformedReference);
            // Calculate the position the camera is looking at.
            Vector3.Add(ref cameraPosition, ref transformedReference,
                out cameraTarget);
        }

        public void InitializeTransformWithCalculation()
        {
            float aspectRatio = (float)640 / (float)480;
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), aspectRatio, 0.0001f, 1000.0f);
            view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
        }

        public void InitializeTransformWithConstant()
        {
            float aspectRatio = (float)640 / (float)480;
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, aspectRatio, 0.0001f, 1000.0f);
            view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
        }

        public void InitializeTransformWithDivision()
        {
            float aspectRatio = (float)640 / (float)480;
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.Pi / 4, aspectRatio, 0.0001f, 1000.0f);
            view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
        }

        public void InitializeTransformWithConstantReferenceOut()
        {
            float aspectRatio = (float)640 / (float)480;
            Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), aspectRatio, 0.0001f, 1000.0f,
                out projection);
            Matrix.CreateLookAt(
                ref cameraPosition, ref cameraTarget, ref vectorUp, out view);
        }

        public void InitializeTransformWithPreDeterminedAspectRatio()
        {
            Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), 1.33333f, 0.0001f, 1000.0f,
                out projection);
            Matrix.CreateLookAt(
                ref cameraPosition, ref cameraTarget, ref vectorUp, out view);
        }

        public void CreateCameraReferenceWithProperty()
        {
            Vector3 cameraReference = Vector3.Forward;
            Matrix rotationMatrix;
            Matrix.CreateRotationY(
                MathHelper.ToRadians(45.0f), out rotationMatrix);
            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference;
            Vector3.Transform(ref cameraReference, ref rotationMatrix,
                out transformedReference);
            // Calculate the position the camera is looking at.
            cameraTarget = cameraPosition + transformedReference;
        }

        public void CreateCameraReferenceWithValue()
        {
            Vector3 cameraReference = new Vector3(0, 0, -1.0f);
            Matrix rotationMatrix;
            Matrix.CreateRotationY(
                MathHelper.ToRadians(45.0f), out rotationMatrix);
            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference;
            Vector3.Transform(ref cameraReference, ref rotationMatrix,
                out transformedReference);
            // Calculate the position the camera is looking at.
            cameraTarget = cameraPosition + transformedReference;
        }

        public void RotateWithoutMod()
        {
            cameraYaw += 2.0f;

            if (cameraYaw > 360)
                cameraYaw -= 360;
            if (cameraYaw < 0)
                cameraYaw += 360;

            float tmp = cameraYaw;
        }

        public void RotateWithMod()
        {
            cameraYaw += 2.0f;

            cameraYaw %= 360;

            float tmp = cameraYaw;
        }

        public void RotateElseIf()
        {
            cameraYaw += 2.0f;

            if (cameraYaw > 360)
                cameraYaw -= 360;
            else if (cameraYaw < 0)
                cameraYaw += 360;

            float tmp = cameraYaw;
        }
    }
}
