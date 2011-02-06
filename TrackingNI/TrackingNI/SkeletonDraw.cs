using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xn;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace TrackingNI
{
    public class SkeletonDraw
    {
        private DepthGenerator depthGenerator;

        public void DrawStickFigure(ref WriteableBitmap image, DepthGenerator depthGenerator, DepthMetaData data, UserGenerator userGenerator)
        {
            Point3D corner = new Point3D(data.XRes, data.YRes, data.ZRes);
            corner = depthGenerator.ConvertProjectiveToRealWorld(corner);
            this.depthGenerator = depthGenerator;

            int nXRes = data.XRes;
            int nYRes = data.YRes;

            uint[] users = userGenerator.GetUsers();
            foreach (uint user in users)
            {
                //Console.Write("Found User " + user);
                if (userGenerator.GetSkeletonCap().IsTracking(user))
                {
                    Console.Write("Tracking User " + user);
                    DrawSingleUser(ref image, user, userGenerator, corner);
                }
            }
        }

        public void DrawSingleUser(ref WriteableBitmap image, uint id, UserGenerator userGenerator, Point3D corner)
        {
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftHand, SkeletonJoint.LeftElbow, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftHand, SkeletonJoint.LeftElbow, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftElbow, SkeletonJoint.LeftShoulder, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftShoulder, SkeletonJoint.Torso, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftShoulder, SkeletonJoint.RightShoulder, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.Torso, SkeletonJoint.RightShoulder, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.RightShoulder, SkeletonJoint.RightElbow, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.RightElbow, SkeletonJoint.RightHand, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.Neck, SkeletonJoint.Head, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.Torso, SkeletonJoint.LeftHip, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.Torso, SkeletonJoint.RightHip, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftHip, SkeletonJoint.RightHip, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.LeftKnee, SkeletonJoint.LeftFoot, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.RightHip, SkeletonJoint.RightKnee, corner);
            DrawStickLine(ref image, id, userGenerator, SkeletonJoint.RightKnee, SkeletonJoint.RightFoot, corner);

            SkeletonJointPosition leftShoulder = new SkeletonJointPosition();
            SkeletonJointPosition rightShoulder = new SkeletonJointPosition();
            SkeletonJointPosition neck = new SkeletonJointPosition();
            SkeletonJointPosition midShoulder = new SkeletonJointPosition();

            userGenerator.GetSkeletonCap().GetSkeletonJointPosition(id, SkeletonJoint.LeftShoulder, ref leftShoulder);
            userGenerator.GetSkeletonCap().GetSkeletonJointPosition(id, SkeletonJoint.RightShoulder, ref rightShoulder);
            userGenerator.GetSkeletonCap().GetSkeletonJointPosition(id, SkeletonJoint.Neck, ref neck);

            midShoulder.position = new Point3D((leftShoulder.position.X + rightShoulder.position.X) / 2,
                                               (leftShoulder.position.Y + rightShoulder.position.Y) / 2,
                                               (leftShoulder.position.Z + rightShoulder.position.Z) / 2);
            midShoulder.fConfidence = (leftShoulder.fConfidence + rightShoulder.fConfidence) / 2;

            /*
            DrawStickPoint(ref image, neck, corner);
            DrawStickPoint(ref image, midShoulder, corner);

            foreach (SkeletonJoint joint in Enum.GetValues(typeof(SkeletonJoint)))
            {
                DrawOrientation(ref image, id, userGenerator, joint, corner);
            }
            */
        }

        public void DrawStickLine(ref WriteableBitmap image, uint id, UserGenerator userGenerator, SkeletonJoint first, SkeletonJoint second, Point3D corner)
        {
            SkeletonJointPosition a = new SkeletonJointPosition();
            SkeletonJointPosition b = new SkeletonJointPosition();

            userGenerator.GetSkeletonCap().GetSkeletonJointPosition(id, first, ref a);
            userGenerator.GetSkeletonCap().GetSkeletonJointPosition(id, second, ref b);

            if (a.fConfidence == 1 && b.fConfidence == 1)
            {
                // choose color
            }
            else
            {
                if ((a.position.X == 0 && a.position.Y == 0 && a.position.Z == 0) ||
                    (b.position.X == 0 && b.position.Y == 0 && b.position.Z == 0))
                {
                    return;
                }
            }

            DrawTheLine(ref image, ref a, ref b);
            /*
            DrawStickPoint(ref image, a, corner);
            DrawStickPoint(ref image, b, corner);
            */
        }

        public void DrawTheLine(ref WriteableBitmap image, ref SkeletonJointPosition joint1, ref SkeletonJointPosition joint2)
        {
            image.Lock();

            //Point3D point1 = depthGenerator.ConvertProjectiveToRealWorld(joint1.position);
            //Point3D point2 = depthGenerator.ConvertProjectiveToRealWorld(joint2.position);

            var b = new Bitmap(image.PixelWidth, image.PixelHeight, image.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                image.BackBuffer);

            using (var bitmapGraphics = System.Drawing.Graphics.FromImage(b))
            {
                bitmapGraphics.SmoothingMode = SmoothingMode.HighSpeed;
                bitmapGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                bitmapGraphics.CompositingMode = CompositingMode.SourceCopy;
                bitmapGraphics.CompositingQuality = CompositingQuality.HighSpeed;
                //bitmapGraphics.DrawLine(Pens.Gold, (point1.X+640)/2, (point1.Y+480)/2,(point2.X+640)/2,(point2.Y+480)/2);
                bitmapGraphics.DrawLine(Pens.BlueViolet, (joint1.position.X + 640) / 2, 480 - (joint1.position.Y + 480) / 2, (joint2.position.X + 640) / 2, 480 - (joint2.position.Y + 480) / 2);
                bitmapGraphics.Dispose();
            }
            image.AddDirtyRect(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight));
            image.Unlock();

        }

        public void DrawStickPoint(ref WriteableBitmap image, SkeletonJointPosition joint, Point3D corner)
        {
            //Console.WriteLine(joint.position.X + " " + joint.position.Y + " " + joint.position.Z);
            byte[] point = { 0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0,
                             0, 0, 255, 0, };

            image.Lock();
        
            image.WritePixels(new Int32Rect(Convert.ToInt32(joint.position.X- 1),
                                            Convert.ToInt32(joint.position.Y - 1),
                                            3, 3), point, 4, 0);
      
            image.Unlock();
        }

        public void DrawOrientation(ref WriteableBitmap image, uint id, UserGenerator userGenerator, SkeletonJoint joint, Point3D corner)
        {
            SkeletonJointOrientation orientation = new SkeletonJointOrientation();
            SkeletonJointPosition position = new SkeletonJointPosition();

            userGenerator.GetSkeletonCap().GetSkeletonJointPosition(id, joint, ref position);
            userGenerator.GetSkeletonCap().GetSkeletonJointOrientation(id, joint, orientation);

            if (position.fConfidence != 1 && orientation.Confidence != 1)
            {
                return;
            }

            SkeletonJointPosition v1 = new SkeletonJointPosition();
            SkeletonJointPosition v2 = new SkeletonJointPosition();
            v1.fConfidence = v2.fConfidence = 1;

            v1.position = position.position;
            v2.position = new Point3D(v1.position.X + 100 * orientation.Orientation.elements[0],
                                      v1.position.Y + 100 * orientation.Orientation.elements[3],
                                      v1.position.Z + 100 * orientation.Orientation.elements[6]);

            DrawStickPoint(ref image, v1, corner);
            DrawStickPoint(ref image, v2, corner);

            v1.position = position.position;
            v2.position = new Point3D(v1.position.X + 100 * orientation.Orientation.elements[1],
                                      v1.position.Y + 100 * orientation.Orientation.elements[4],
                                      v1.position.Z + 100 * orientation.Orientation.elements[7]);

            DrawStickPoint(ref image, v1, corner);
            DrawStickPoint(ref image, v2, corner);

            v1.position = position.position;
            v2.position = new Point3D(v1.position.X + 100 * orientation.Orientation.elements[2],
                                      v1.position.Y + 100 * orientation.Orientation.elements[5],
                                      v1.position.Z + 100 * orientation.Orientation.elements[8]);

            DrawStickPoint(ref image, v1, corner);
            DrawStickPoint(ref image, v2, corner);
        }
    }
}
