using System;
using System.IO;
using _Game.GameGrid.GridSurface;
using _Game.GameGrid.Unit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VinhLB
{
    public static class GridMapFileUtilities
    {
        public static void Save(string name)
        {
            switch (name)
            {
                case null:
                case " ":
                case "":
                    return;
            }

            GridSurface[] gridSurfaces = Object.FindObjectsOfType<GridSurface>();
            if (gridSurfaces.Length == 0)
            {
                Debug.LogError("Grid must have at least 1 surface, and all unit must have on a surface");
                return;
            }

            GridUnit[] gridUnits = Object.FindObjectsOfType<GridUnit>();
            int minX = int.MaxValue;
            int minZ = int.MaxValue;
            int maxX = int.MinValue;
            int maxZ = int.MinValue;
            foreach (GridSurface gridSurface in gridSurfaces)
            {
                Vector3 position = gridSurface.Tf.position;
                if (position.x < minX) minX = (int)Math.Round(position.x);
                if (position.z < minZ) minZ = (int)Math.Round(position.z);
                if (position.x > maxX) maxX = (int)Math.Round(position.x);
                if (position.z > maxZ) maxZ = (int)Math.Round(position.z);
            }

            // if minX or minY < 11, get the offset and make all position added with this offset so the minX and minY can be 11 (index 1,1)
            if (minX < 11)
            {
                int offsetX = 11 - minX;
                foreach (GridSurface gridSurface in gridSurfaces)
                {
                    Vector3 position = gridSurface.Tf.position;
                    position.x += offsetX;
                    gridSurface.Tf.position = position;
                }

                foreach (GridUnit gridUnit in gridUnits)
                {
                    Vector3 position = gridUnit.Tf.position;
                    position.x += offsetX;
                    gridUnit.Tf.position = position;
                }

                minX += offsetX;
                maxX += offsetX;
            }

            maxX += 10; // add one more cell to maxX and maxY
            if (minZ < 11)
            {
                int offsetZ = 11 - minZ;
                foreach (GridSurface gridSurface in gridSurfaces)
                {
                    Vector3 position = gridSurface.Tf.position;
                    position.z += offsetZ;
                    gridSurface.Tf.position = position;
                }

                foreach (GridUnit gridUnit in gridUnits)
                {
                    Vector3 position = gridUnit.Tf.position;
                    position.z += offsetZ;
                    gridUnit.Tf.position = position;
                }

                minZ += offsetZ;
                maxZ += offsetZ;
            }

            maxZ += 10;
            const int cellOffset = 1;
            maxX = (maxX + cellOffset) / 2;
            maxZ = (maxZ + cellOffset) / 2;
            // create 2 dimension int array with default value is 0
            int[,] gridData = new int[maxX, maxZ];
            // fill the array with 0
            for (int i = 0; i < maxX; i++)
            for (int j = 0; j < maxZ; j++)
                gridData[i, j] = 0;
            // Get position of each gridSurface and set the value of gridSurfaceData to 1
            foreach (GridSurface gridSurface in gridSurfaces)
            {
                Vector3 position = gridSurface.Tf.position;
                int x = (int)(position.x + 1) / 2;
                int z = (int)(position.z + 1) / 2;
                gridData[x - 1, z - 1] = (int)gridSurface.PoolType;
            }

            // Save the array as txt file in Resources folder
            string path = "Assets/_Game/Resources/" + name + ".txt";
            File.WriteAllText(path, string.Empty);
            using StreamWriter file = new(path, true);
            for (int i = 0; i < maxX; i++)
            {
                string line = "";
                for (int j = 0; j < maxZ; j++) line += gridData[i, j] + " ";
                line = line.Remove(line.Length - 1);
                file.WriteLine(line);
            }

            Debug.Log("Save surface: Complete");
            // Reset the array to all 0
            for (int i = 0; i < maxX; i++)
            for (int j = 0; j < maxZ; j++)
                gridData[i, j] = 0;
            // Handle gridUnit
            // Set position of gritUnit to gridData
            foreach (GridUnit gridUnit in gridUnits)
            {
                Vector3 position = gridUnit.Tf.position;
                int x = (int)(position.x + 1) / 2;
                int z = (int)(position.z + 1) / 2;
                // if x or z larger than size of array, return 
                if (x > maxX || z > maxZ)
                {
                    Debug.LogError("Grid Unit must be on Grid Surface");
                    // Close the file then delete it
                    file.Close();
                    File.Delete(path);
                    // Remove the file
                    return;
                }

                gridData[x - 1, z - 1] = (int)gridUnit.PoolType;
            }

            // Write a @ to separate
            file.WriteLine("@");
            // Save the array as txt file in Resources folder
            for (int i = 0; i < maxX; i++)
            {
                string line = "";
                for (int j = 0; j < maxZ; j++) line += gridData[i, j] + " ";
                line = line.Remove(line.Length - 1);
                file.WriteLine(line);
            }

            Debug.Log("Save unit: Complete");

            // Reset the array to all -1
            for (int i = 0; i < maxX; i++)
            for (int j = 0; j < maxZ; j++)
                gridData[i, j] = -1;
            // Handle gridUnitRotationDirection
            foreach (GridUnit gridUnit in gridUnits)
            {
                Vector3 position = gridUnit.Tf.position;
                int x = (int)(position.x + 1) / 2;
                int z = (int)(position.z + 1) / 2;
                // if x or z larger than size of array, return 
                if (x > maxX || z > maxZ)
                {
                    Debug.LogError("Grid Unit must be on Grid Surface");
                    // Close the file then delete it
                    file.Close();
                    File.Delete(path);
                    // Remove the file
                    return;
                }

                gridData[x - 1, z - 1] = (int)gridUnit.SkinRotationDirection;
            }

            // Write a @ to separate
            file.WriteLine("@");
            // Save the array as txt file in Resources folder
            for (int i = 0; i < maxX; i++)
            {
                string line = "";
                for (int j = 0; j < maxZ; j++) line += gridData[i, j] + " ";
                line = line.Remove(line.Length - 1);
                file.WriteLine(line);
            }

            file.Close();
            Debug.Log("Save unit rotation: Complete");
        }
    }
}
