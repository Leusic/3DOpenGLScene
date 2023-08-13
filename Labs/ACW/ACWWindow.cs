using Labs.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Labs.ACW
{
    public class ACWWindow : GameWindow
    {
        public ACWWindow()
            : base(
                800, // Width
                600, // Height
                GraphicsMode.Default,
                "Assessed Coursework",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                3, // major
                3, // minor
                GraphicsContextFlags.ForwardCompatible
                )
        {
        }

        private int[] mVBO_IDs = new int[16];
        private int[] mVAO_IDs = new int[10];
        private int mTexture_ID, mTexture_ID2;
        private ShaderUtility mShader;
        private ModelUtility mDilloModelUtility, mCylinderModelUtility;
        private Matrix4 mView, mDilloModel, mCylinderModel, mGroundModel, mBackWallModel, mLeftWallModel, mRightWallModel, mCubeModel, mPyramidModel;
        private Vector4[] lightPositions = new Vector4[3];
        Bitmap TextureBitmap, TextureBitmap2;
        BitmapData TextureData, TextureData2;

        public void setMaterial(Vector3 ambient, Vector3 diffuse, Vector3 specular, float shiny)
        {
            int uAmbientReflectivityLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uMaterial.AmbientReflectivity");
            GL.Uniform3(uAmbientReflectivityLocation, ambient);

            int uDiffuseReflectivityLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uMaterial.DiffuseReflectivity");
            GL.Uniform3(uDiffuseReflectivityLocation, diffuse);

            int uSpecularReflectivityLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uMaterial.SpecularReflectivity");
            GL.Uniform3(uSpecularReflectivityLocation, specular);

            int uShininessLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uMaterial.Shininess");
            GL.Uniform1(uShininessLocation, shiny * 128);
        }

        public void setTexture(string filepath, Bitmap texBitmap, BitmapData texData, int mTex_ID, int textureUnit)
        {
            if (System.IO.File.Exists(filepath))
            {
                texBitmap = new Bitmap(filepath);
                texBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                texData = texBitmap.LockBits(new System.Drawing.Rectangle(0, 0, texBitmap.Width, texBitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            }
            else
            {
                throw new Exception("Could not find file " + filepath);
            }

            GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
            GL.GenTextures(1, out mTex_ID);
            GL.BindTexture(TextureTarget.Texture2D, mTex_ID);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texData.Width, texData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, texData.Scan0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            texBitmap.UnlockBits(texData);

            string textureSampler = "uTextureSampler" + (textureUnit + 1);
            int uTextureSamplerLocation = GL.GetUniformLocation(mShader.ShaderProgramID, textureSampler);
            GL.Uniform1(uTextureSamplerLocation, textureUnit);

            int uTextureNumLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uTextureNum");
            GL.Uniform1(uTextureNumLocation, textureUnit);
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            mShader = new ShaderUtility(@"..\..\ACW\Shaders\acwPassThrough.vert", @"..\..\ACW\Shaders\acwLighting.frag");
            GL.UseProgram(mShader.ShaderProgramID);

            mView = Matrix4.CreateTranslation(0, -3.5f, 0);
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);

            //int uTextureSamplerLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uTextureSampler");
            //GL.Uniform1(uTextureSamplerLocation, 0);

            //uTextureSamplerLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uTextureSampler2");
            //GL.Uniform1(uTextureSamplerLocation, 1);

            int vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            int vNormalLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vNormal");
            int uEyePositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            Vector4 cameraPosition = Vector4.Transform(new Vector4(0, 0, 0, 1), mView);
            GL.Uniform4(uEyePositionLocation, cameraPosition);

            //Light 1

            int uLightPositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[0].Position");
            Vector4 lightPosition = new Vector4(-4, 6, -8.5f, 1);
            lightPositions[0] = lightPosition;
            lightPosition = Vector4.Transform(lightPosition, mView);
            GL.Uniform4(uLightPositionLocation, lightPosition);

            int uAmbientLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[0].AmbientLight");
            Vector3 colour = new Vector3(0.1f, 0.0f, 0.0f);
            GL.Uniform3(uAmbientLightLocation, colour);

            int uDiffuseLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[0].DiffuseLight");
            colour = new Vector3(0f, 0f, 1f);
            GL.Uniform3(uDiffuseLightLocation, colour);

            int uSpecularLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[0].SpecularLight");
            colour = new Vector3(1.0f, 1.0f, 1.0f);
            GL.Uniform3(uSpecularLightLocation, colour);

            //Light 2

            uLightPositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[1].Position");
            lightPosition = new Vector4(0, 6, -8f, 1);
            lightPositions[1] = lightPosition;
            lightPosition = Vector4.Transform(lightPosition, mView);
            GL.Uniform4(uLightPositionLocation, lightPosition);

            uAmbientLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[1].AmbientLight");
            colour = new Vector3(0.1f, 0.0f, 0.0f);
            GL.Uniform3(uAmbientLightLocation, colour);

            uDiffuseLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[1].DiffuseLight");
            colour = new Vector3(1f, 0f, 0f);
            GL.Uniform3(uDiffuseLightLocation, colour);

            uSpecularLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[1].SpecularLight");
            colour = new Vector3(1.0f, 1.0f, 1.0f);
            GL.Uniform3(uSpecularLightLocation, colour);

            //Light 3

            uLightPositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[2].Position");
            lightPosition = new Vector4(4, 6, -8.5f, 1);
            lightPositions[2] = lightPosition;
            lightPosition = Vector4.Transform(lightPosition, mView);
            GL.Uniform4(uLightPositionLocation, lightPosition);

            uAmbientLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[2].AmbientLight");
            colour = new Vector3(0.1f, 0.0f, 0.0f);
            GL.Uniform3(uAmbientLightLocation, colour);

            uDiffuseLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[2].DiffuseLight");
            colour = new Vector3(0f, 1f, 0f);
            GL.Uniform3(uDiffuseLightLocation, colour);

            uSpecularLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[2].SpecularLight");
            colour = new Vector3(1.0f, 1.0f, 1.0f);
            GL.Uniform3(uSpecularLightLocation, colour);

            GL.GenVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);

            //Floor

            float[] vertices = new float[] {-10, 0, -10,0,1,0,0,0,
                                             -10, 0, 10,0,1,0,0,1,
                                             10, 0, 10,0,1,0,1,1,
                                             10, 0, -10,0,1,0,1,0,
            };

            GL.BindVertexArray(mVAO_IDs[0]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 3 * sizeof(float));

            int vTexCoordsLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vTexCoords");
            GL.EnableVertexAttribArray(vTexCoordsLocation);
            GL.VertexAttribPointer(vTexCoordsLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            //Back Wall

            vertices = new float[] {
                                            10, 0, -10,0,1,0,1,0,
                                            10, 10, -10,0,1,0,1,1,
                                            -10, 10, -10, 0,1,0,0,1,
                                            -10,0,-10,0,1,0,0,0,
            };

            GL.BindVertexArray(mVAO_IDs[3]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[5]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 3 * sizeof(float));

            vTexCoordsLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vTexCoords");
            GL.EnableVertexAttribArray(vTexCoordsLocation);
            GL.VertexAttribPointer(vTexCoordsLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            //Left Wall

            vertices = new float[] {
                                            -10,10,10, 0,1,0,1,0,
                                            -10,0,10,0,1,0,1,1,
                                            -10,0,-10,0,1,0,0,1,
                                            -10,10,-10,0,1,0,0,0,

            };

            GL.BindVertexArray(mVAO_IDs[4]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[6]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 3 * sizeof(float));

            vTexCoordsLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vTexCoords");
            GL.EnableVertexAttribArray(vTexCoordsLocation);
            GL.VertexAttribPointer(vTexCoordsLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            //Right Wall

            vertices = new float[] {
                                            10,0,10,0,1,0,1,1,
                                            10,10,10,0,1,0,1,0,
                                            10,10,-10,0,1,0,0,0,
                                            10,0,-10,0,1,0,0,1,
            };

            GL.BindVertexArray(mVAO_IDs[5]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[7]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 3 * sizeof(float));

            vTexCoordsLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vTexCoords");
            GL.EnableVertexAttribArray(vTexCoordsLocation);
            GL.VertexAttribPointer(vTexCoordsLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            //Armadillo

            mDilloModelUtility = ModelUtility.LoadModel(@"Utility/Models/model.bin");

            GL.BindVertexArray(mVAO_IDs[1]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[1]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mDilloModelUtility.Vertices.Length * sizeof(float)), mDilloModelUtility.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[2]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mDilloModelUtility.Indices.Length * sizeof(float)), mDilloModelUtility.Indices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mDilloModelUtility.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mDilloModelUtility.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            //Cylinder

            mCylinderModelUtility = ModelUtility.LoadModel(@"Utility/Models/cylinder.bin");

            GL.BindVertexArray(mVAO_IDs[2]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[3]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mCylinderModelUtility.Vertices.Length * sizeof(float)), mCylinderModelUtility.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[4]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mCylinderModelUtility.Indices.Length * sizeof(float)), mCylinderModelUtility.Indices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mCylinderModelUtility.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mCylinderModelUtility.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            //Primitive Pyramid

            vertices = new float[] {
                0,1,0,0,1,0,
                -1,-1,1,0,1,0,
                1,-1,1,0,1,0,

                0,1,0,0,1,0,
                1,-1,1,0,1,0,
                1,-1,-1,0,1,0,

                0,1,0,0,1,0,
                1,-1,-1,0,1,0,
                -1,-1,-1,0,1,0,

                0,1,0,0,1,0,
                -1,-1,-1,0,1,0,
                -1,-1,1,0,1,0,                                        
            };

            GL.BindVertexArray(mVAO_IDs[7]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[9]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            //Cube

            vertices = new float[] {

                                            -1,0,-1,0,1,0,
                                            -1, 2, -1, 0,1,0,
                                            1, 2, -1,0,1,0,
                                            1, 0, -1,0,1,0,

                                            1,0,-1,0,1,0,
                                            1, 2, -1,0,1,0,
                                            1, 2, 1, 0,1,0,
                                            1,0,1,0,1,0,

                                            -1,0,1,0,1,0,
                                            -1, 2, 1, 0,1,0,
                                            -1, 2, -1,0,1,0,
                                            -1,0,-1,0,1,0,

                                            1,0,1,0,1,0,
                                            1,2,1,0,1,0,
                                            -1,2,1,0,1,0,
                                            -1,0,1,0,1,0,

                                            -1,0,1,0,1,0,
                                            -1,0,-1,0,1,0,
                                            1,0,-1,0,1,0,
                                            1,0,1,0,1,0,

                                            1,2,1,0,1,0,
                                            1,2,-1,0,1,0,
                                            -1,2,-1,0,1,0,
                                            -1,2,1,0,1,0,

            };

            GL.BindVertexArray(mVAO_IDs[6]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[8]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));


            //Setting Positions

            GL.BindVertexArray(0);

            mGroundModel = Matrix4.CreateTranslation(0, 0, -5f);
            mDilloModel = Matrix4.CreateTranslation(0, 2.5f, -5f);
            mCylinderModel = Matrix4.CreateTranslation(0, 0.5f, -5f);
            mBackWallModel = Matrix4.CreateTranslation(0, 0, -5f);
            mLeftWallModel = Matrix4.CreateTranslation(0, 0, -5f);
            mRightWallModel = Matrix4.CreateTranslation(0, 0, -5f);
            mCubeModel = Matrix4.CreateTranslation(-5, 1.0f, -10.5f);
            mPyramidModel = Matrix4.CreateTranslation(5, 1.0f, -10.5f);
            base.OnLoad(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.KeyChar == 'w')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.0f, 0.05f);
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
                int uEyePositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
                GL.UniformMatrix4(uEyePositionLocation, true, ref mView);
            }
            if (e.KeyChar == 'a')
            {
                mView = mView * Matrix4.CreateRotationY(-0.025f);
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
                int uEyePositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
                GL.UniformMatrix4(uEyePositionLocation, true, ref mView);
            }
            if (e.KeyChar == 's')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.0f, -0.05f);
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
                int uEyePositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
                GL.UniformMatrix4(uEyePositionLocation, true, ref mView);
            }
            if (e.KeyChar == 'd')
            {
                mView = mView * Matrix4.CreateRotationY(0.025f);
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
                int uEyePositionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
                GL.UniformMatrix4(uEyePositionLocation, true, ref mView);
            }
            if (e.KeyChar == 'c')
            {
                Vector3 t = mDilloModel.ExtractTranslation();
                Matrix4 translation = Matrix4.CreateTranslation(t);
                Matrix4 inverseTranslation = Matrix4.CreateTranslation(-t);
                mDilloModel = mDilloModel * inverseTranslation * Matrix4.CreateRotationY(-0.025f) * translation;
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
            }
            if (e.KeyChar == 'v')
            {
                Vector3 t = mDilloModel.ExtractTranslation();
                Matrix4 translation = Matrix4.CreateTranslation(t);
                Matrix4 inverseTranslation = Matrix4.CreateTranslation(-t);
                mDilloModel = mDilloModel * inverseTranslation * Matrix4.CreateRotationY(0.025f) * translation;
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
            }
            for (int i = 0; i < 3; ++i)
            {
                Vector4 transLightPosition = Vector4.Transform(new Vector4(lightPositions[i].X, lightPositions[i].Y, lightPositions[i].Z, 1), mView);
                string attributeAddressString = "uLight[" + i + "].Position";
                int uLightDirectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, attributeAddressString);
                GL.Uniform4(uLightDirectionLocation, transLightPosition);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
            if (mShader != null)
            {
                int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.5f, 25);
                GL.UniformMatrix4(uProjectionLocation, true, ref projection);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
 	        base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //Ground

            setTexture(@"..\..\ACW\stone2.jpg", TextureBitmap, TextureData, mTexture_ID, 0);
            setMaterial(new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.55f, 0.55f), new Vector3(0.7f, 0.7f, 0.7f), 0.25f);

            int uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mGroundModel);

            GL.BindVertexArray(mVAO_IDs[0]);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            //Back Wall

            setTexture(@"..\..\ACW\stone2.jpg", TextureBitmap2, TextureData2, mTexture_ID2, 1);

            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mBackWallModel);

            GL.BindVertexArray(mVAO_IDs[3]);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            //Left Wall

            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mLeftWallModel);

            GL.BindVertexArray(mVAO_IDs[4]);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            //Right Wall

            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mRightWallModel);

            GL.BindVertexArray(mVAO_IDs[5]);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            //Armadillo

            setMaterial(new Vector3(0.2125f, 0.1275f, 0.054f), new Vector3(0.714f, 0.4284f, 0.18144f), new Vector3(0.393548f, 0.271906f, 0.166721f), 0.2f);

            Matrix4 m = mDilloModel * mGroundModel;
            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref m);

            GL.BindVertexArray(mVAO_IDs[1]);
            GL.DrawElements(PrimitiveType.Triangles, mDilloModelUtility.Indices.Length, DrawElementsType.UnsignedInt, 0);

            //Cylinder

            setMaterial(new Vector3(0.05375f, 0.05f, 0.06625f), new Vector3(0.18275f, 0.17f, 0.22525f), new Vector3(0.332741f, 0.328634f, 0.346435f), 0.1f);

            m = mCylinderModel * mGroundModel;
            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref m);

            GL.BindVertexArray(mVAO_IDs[2]);
            GL.DrawElements(PrimitiveType.Triangles, mCylinderModelUtility.Indices.Length, DrawElementsType.UnsignedInt, 0);

            //Cube

            setMaterial(new Vector3(0.1745f, 0.01175f, 0.01175f), new Vector3(0.61424f, 0.04136f, 0.04136f), new Vector3(0.727811f, 0.626959f, 0.626959f), 0.6f);

            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mCubeModel);

            GL.BindVertexArray(mVAO_IDs[6]);
            GL.DrawArrays(PrimitiveType.Quads, 0, 24);

            //Pyramid

            setMaterial(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.55f, 0.55f, 0.55f), new Vector3(0.7f, 0.7f, 0.7f), 0.25f);

            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mPyramidModel);

            GL.BindVertexArray(mVAO_IDs[7]);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 16);

            GL.BindVertexArray(0);
            this.SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.DeleteVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            mShader.Delete();
            base.OnUnload(e);
        }
    }
}
