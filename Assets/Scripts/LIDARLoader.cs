using UnityEngine;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System;

public class LIDARLoader : MonoBehaviour
{

    public string filesDir = "LIDAR-DTM-2M-TQ38";
    public ComputeShader shader;
    public RenderTexture[] outputTex;

    public Material material;

    private ComputeBuffer[] buffer;
    private ComputeBuffer pointBuffer;

    private ComputeBuffer renderBuffer;

    //public RenderTexture dataTex;

    public struct TileCPUData
    {
        public Bounds bounds;
        public bool visible;
    }

    private TileCPUData[] tiles;

    int computeKernelHandle;

    int totalPoints = 500 * 500;

    public Texture2D[] inputTex;

    int visibleTiles = 0;

    int loadedFiles = 0;

    // Use this for initialization
    void Start()
    {
        computeKernelHandle = shader.FindKernel("CSMain");
  
        LoadAllFiles();

        inputTex = new Texture2D[loadedFiles];
        
        pointBuffer = new ComputeBuffer(totalPoints * loadedFiles, sizeof(float) * 3, ComputeBufferType.Append);
        renderBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);

        int[] renderArgs = new int[4];
        renderArgs[0] = 250000*loadedFiles; renderArgs[1] = 1; renderArgs[2] = 0; renderArgs[3] = 0;

        renderBuffer.SetData(renderArgs);

        shader.SetBuffer(computeKernelHandle, "renderBuffer", renderBuffer);
        shader.SetBuffer(computeKernelHandle, "points", pointBuffer);

        for (int i = 0; i < loadedFiles;i++)
        {
            byte[] byteArray = new byte[totalPoints * sizeof(float)];
            Buffer.BlockCopy(dataArray[i], 0, byteArray, 0, byteArray.Length);

            inputTex[i] = new Texture2D(500, 500, TextureFormat.RFloat, false);
            inputTex[i].LoadRawTextureData(byteArray);
            inputTex[i].Apply();
        }

        FrustrumCullTiles();

        for (int i = 0; i < loadedFiles; i++)
        {
            shader.SetTexture(computeKernelHandle, "input", inputTex[i]);
            shader.SetBuffer(computeKernelHandle, "tileData", buffer[i]);

            shader.Dispatch(computeKernelHandle, 500, 1, 1);
        }
    }

    void FrustrumCullTiles()
    {
        visibleTiles = 0;

        for (int i = 0; i < loadedFiles; i++)
        {
            Vector3 tMinVec = Camera.main.WorldToViewportPoint(tiles[i].bounds.min);
            Vector3 tMaxVec = Camera.main.WorldToViewportPoint(tiles[i].bounds.max);

            Bounds tBounds = new Bounds();
            tBounds.SetMinMax(tMinVec, tMaxVec);

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(planes, tiles[i].bounds))
            {
                tiles[i].visible = true;
                visibleTiles++;
            }

        }
    }

    void LoadAllFiles()
    {
        DirectoryInfo dir = new DirectoryInfo(filesDir);

        FileInfo[] files = dir.GetFiles();

        dataArray = new float[files.Length][];
        buffer = new ComputeBuffer[files.Length];
        tiles = new TileCPUData[files.Length];

        int fileCounter = 0;
        for (int i = 0; i < files.Length; i++)
        {
            print("Reading File " + files[i].FullName);
            ReadFile(files[i], i);

            fileCounter++;

            if (fileCounter > 9)
                return;
        }
    }

    float[][] dataArray = null;

    void ReadFile(FileInfo fileInfo, int fileCounter)
    {
        int counter = 0;
        string line = "";

        int valueLineCounter = 0;

        int nCols = 0, nRows = 0;
        int xlCorner = 0, ylCorner = 0;
        int cellSize = 0, nullData = 0;

        print("Reading File " + fileCounter);

        Vector3 minVec = new Vector3();
        Vector3 maxVec = new Vector3();

        

        System.IO.StreamReader file = new System.IO.StreamReader(fileInfo.FullName);
        while ((line = file.ReadLine()) != null)
        {
            //print(line);

            if (counter < 6)
            {
                // Reading Header

                // 0 NCols
                // 1 NRows
                // 2 xlCorner
                // 3 YL Corner
                // 4 CellSize
                // 5 NODATA_VALUE

                string cleanLine = Regex.Replace(line, @"\s+", "");

                if (cleanLine.Contains("ncols"))
                    nCols = int.Parse(cleanLine.Replace("ncols", ""));

                if (cleanLine.Contains("nrows"))
                    nRows = int.Parse(cleanLine.Replace("nrows", ""));

                if (cleanLine.Contains("xllcorner"))
                    xlCorner = int.Parse(cleanLine.Replace("xllcorner", ""));

                if (cleanLine.Contains("yllcorner"))
                    ylCorner = int.Parse(cleanLine.Replace("yllcorner", ""));

                if (cleanLine.Contains("cellsize"))
                    cellSize = int.Parse(cleanLine.Replace("cellsize", ""));

                if (cleanLine.Contains("NODATA_value"))
                    nullData = int.Parse(cleanLine.Replace("NODATA_value", ""));

                print("nCols " + nCols + " nRows " + nRows + " xlCorner " + xlCorner + " ylCorner " + ylCorner + " cellSize " + cellSize + " NODATA_VALUE " + nullData);
            }
            else
            {
                // Depth Data

                float xOffset = xlCorner - 530000;
                float yOffset = ylCorner - 180000;

                if (dataArray[fileCounter] == null)
                {
                    dataArray[fileCounter] = new float[nCols * nRows];

                    float[] offsets = new float[3];
                    offsets[0] = xOffset; offsets[1] = yOffset; offsets[2] = fileCounter;

                    buffer[fileCounter] = new ComputeBuffer(offsets.Length, sizeof(float));
                    buffer[fileCounter].SetData(offsets);

                    minVec = new Vector3(xOffset, -nullData, yOffset);
                    maxVec = new Vector3(xOffset + (nCols * cellSize), nullData, yOffset + (nRows * cellSize));

                    loadedFiles++;
                }

                line = line.Substring(1);

                string[] values = line.Split(' ');

                minVec.y = 0;
                maxVec.y = 0;

                bool doSkip = false;
                for (int i = 0; i < values.Length; i++)
                {
                    float height = float.Parse(values[i]);

                    if (height == nullData)
                        height = 0;

                    if (height < minVec.y)
                        minVec.y = height;
                    else if (height > maxVec.y)
                        maxVec.y = height;

                    dataArray[fileCounter][nCols * valueLineCounter + i] = height;

                }

                valueLineCounter++;
            }

            counter++;
        }

        print("Min " + minVec);
        print ("Max " + maxVec);
        Bounds bounds = new Bounds();
        bounds.SetMinMax(minVec, maxVec);
        tiles[fileCounter].bounds = bounds;

        file.Close();
    }

    void OnDestroy()
    {
        for (int i=0;i<buffer.Length;i++)
            if (buffer[i] != null)
                buffer[i].Release();

        pointBuffer.Release();
        renderBuffer.Release();
    }

    public void OnPostRender()
    {
        //ComputeBuffer.CopyCount(pointBuffer, renderBuffer, 0);

        //Graphics.ClearRandomWriteTargets();

        material.SetPass(0);
        material.SetBuffer("points", pointBuffer);

        //Graphics.DrawProcedural(MeshTopology.Points, totalPoints * loadedFiles, 0);
        //Graphics.DrawProceduralIndirect(MeshTopology.Points, totalPoints * visibleTiles, 0);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, renderBuffer);
    }
}
