using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace JetMod
{
    // Classes part of this mod
    public class Part
    {
        public string partName;
        public bool attached;
        public bool purchased;
        public Vector3 position;
        public Quaternion rotation;
        // Should be in order of bolts length assuming they match
        public List<int> tightness;
    }

    // Save data structure for this mod
    [Serializable]
    public static class SaveData
    {
        // Information about all parts
        public static List<Part> partsList;
        // Fluid level of the fuel tank
        public static float fuelCellFluidLevel = 0f;
        // Information about where the letter is at
        public static Vector3 letterPos = Vector3.zero;
        public static Quaternion letterRot = Quaternion.identity;
        public static bool isOrderPlaced = false;
        public static bool boxSpawned = false;
        public static Vector3 boxPos = Vector3.zero;
        public static Quaternion boxRot = Quaternion.identity;
        public static int timeLeft = 3600;
        public static bool slipAvailable = false;

        // Helper functions related to save information
        public static Part findPart(string name)
        {
            foreach (Part part in partsList)
            {
                if (part.partName.Contains(name))
                {
                    return part;
                }
            }
            ModConsole.LogError("Could not find part " + name);
            return null; // Return null if part with the given name is not found
        }
        public static List<int> grabTightness(OASIS.BasePart part)
        {
            List<int> tightness = new List<int>();
            foreach (OASIS.Bolt bolt in part.bolts)
            {
                tightness.Add(bolt.tightness);
            }
            return tightness;
        }
    }

    // SaveSystem functions
    internal static class SaveSystem
    {
        static readonly string savePath = Application.persistentDataPath + "/JetMod.dat";
        static internal bool gameIsLoaded = false;

        public static void makeOrOverwriteSave()
        {
            if (doesSaveExist()) File.Delete(savePath);
            // Create the save file with using to ensure it disposes properly
            using (File.Create(savePath)) { }
        }

        public static void deleteSave()
        {
            if (doesSaveExist()) File.Delete(savePath);
        }

        public static bool doesSaveBelongToUser(float saveID)
        {
            try
            {
                byte[] encryptedData;

                using (FileStream fileStream = new FileStream(savePath, FileMode.Open))
                {
                    // Read encrypted data from file
                    encryptedData = new byte[fileStream.Length];
                    fileStream.Read(encryptedData, 0, (int)fileStream.Length);
                }

                // Decrypt the data based on player's saveID
                encryptedData = Obfuscate(encryptedData, saveID.ToString());

                // Read decrypted data
                using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                {
                    using (BinaryReader reader = new BinaryReader(memoryStream))
                    {
                        // Save ID unique to all saves
                        float saveidentifier = reader.ReadSingle();
                        return saveID == saveidentifier;
                    }
                }
            }
            catch (IsolatedStorageException)
            {
                return true;
            }
            catch (Exception e)
            {
                ModConsole.LogError("Error trying to read savefile " + e);
                return true;
            }
        }

        public static bool doesSaveExist()
        {
            return File.Exists(savePath);
        }

        public static void Init()
        {
            if (gameIsLoaded) return;
            gameIsLoaded = hasGameLoaded();
        }

        internal static bool hasGameLoaded()
        {
            // Gets folder with appmanifests
            string steamAppsFolderPath = GetParentDirectory(Application.dataPath, 3);

            // Finds appmanifest
            FileInfo info = new FileInfo(steamAppsFolderPath + @"\appmanifest_516750.acf");

            // Compare modified time
            DateTime lastModified = info.LastWriteTime;
            DateTime oneMinuteAgo = DateTime.Now.AddMinutes(-1);

            // This will tell us if the game was opened with Steam.
            return lastModified > oneMinuteAgo;
        }

        static string GetParentDirectory(string path, int levels)
        {
            for (int i = 0; i < levels; i++)
            {
                path = Directory.GetParent(path).FullName;
            }
            return path;
        }

        // Pulls all variables from SaveData and writes them into a file using BinaryWriter
        public static void Save(float saveID)
        {
            byte[] data;
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(memoryStream))
                    {
                        Type type = typeof(SaveData);

                        FieldInfo[] fields = type.GetFields();

                        // Write data unique to this current save
                        writer.Write(saveID);

                        writer.Write(fields.Length);

                        foreach (FieldInfo field in fields)
                        {
                            writer.Write(field.Name);

                            object value = field.GetValue(null);
                            if (value is int valueInt)
                            {
                                writer.Write(valueInt);
                            } else if (value is float floatValue)
                            {
                                writer.Write(floatValue);
                            }
                            else if (value is bool boolValue)
                            {
                                writer.Write(boolValue);
                            }
                            else if (value is string stringValue)
                            {
                                writer.Write(stringValue);
                            } else if (value is Vector3 vectors)
                            {
                                writer.Write(vectors.x);
                                writer.Write(vectors.y);
                                writer.Write(vectors.z);
                            } else if (value is Quaternion quaternion)
                            {
                                writer.Write(quaternion.x);
                                writer.Write(quaternion.y);
                                writer.Write(quaternion.z);
                                writer.Write(quaternion.w);
                            }
                            else if (value is List<int> intListValue)
                            {
                                writer.Write(intListValue.Count);
                                foreach (int intValue in intListValue)
                                {
                                    writer.Write(intValue);
                                }
                            }
                            else if (value is List<Part> partListValue)
                            {
                                writer.Write(partListValue.Count);

                                foreach (Part part in partListValue)
                                {
                                    writer.Write(part.partName);
                                    writer.Write(part.attached);
                                    writer.Write(part.purchased);
                                    writer.Write(part.position.x);
                                    writer.Write(part.position.y);
                                    writer.Write(part.position.z);
                                    writer.Write(part.rotation.x);
                                    writer.Write(part.rotation.y);
                                    writer.Write(part.rotation.z);
                                    writer.Write(part.rotation.w);
                                    writer.Write(part.tightness.Count);
                                    foreach (int tightnessValue in part.tightness)
                                    {
                                        writer.Write(tightnessValue);
                                    }
                                }
                            }
                        }
                    }
                    memoryStream.Flush();

                    // Return the contents of the memory stream as a byte array
                    data = Obfuscate(memoryStream.ToArray(), saveID.ToString());
                }
            }
            catch (Exception e)
            {
                ModConsole.LogError("Failed to save! Report this to the mod creator: " + e);
                return;
            }

            // Write data
            using (FileStream fileStream = new FileStream(savePath, FileMode.Create))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        private static void trySetField(FieldInfo field, BinaryReader reader)
        {
            try
            {
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(null, reader.ReadInt32());
                } else if (field.FieldType == typeof(float))
                {
                    field.SetValue(null, reader.ReadSingle());
                }
                else if (field.FieldType == typeof(bool))
                {
                    field.SetValue(null, reader.ReadBoolean());
                }
                else if (field.FieldType == typeof(string))
                {
                    field.SetValue(null, reader.ReadString());
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    field.SetValue(null, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                }
                else if (field.FieldType == typeof(Quaternion))
                {
                    field.SetValue(null, new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                }
                else if (field.FieldType == typeof(List<int>))
                {
                    int count = reader.ReadInt32();
                    List<int> list = new List<int>();
                    for (int j = 0; j < count; j++)
                    {
                        list.Add(reader.ReadInt32());
                    }
                    field.SetValue(null, list);
                }
                else if (field.FieldType == typeof(List<Part>))
                {
                    int numParts = reader.ReadInt32();

                    var list = new List<Part>();

                    for (int j = 0; j < numParts; j++)
                    {
                        Part part = new Part
                        {
                            partName = reader.ReadString(),
                            attached = reader.ReadBoolean(),
                            purchased = reader.ReadBoolean(),
                            position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                            rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
                        };

                        int tightnessCount = reader.ReadInt32();
                        part.tightness = new List<int>();
                        for (int k = 0; k < tightnessCount; k++)
                        {
                            part.tightness.Add(reader.ReadInt32());
                        }

                        list.Add(part);
                    }
                    field.SetValue(null, list);
                }
            }
            catch (Exception e)
            {
                // Do nothing, SaveData is already initialized for fields it expects to fail
                ModConsole.LogError("JetMod failed to load the variable " + field.Name + ", report this to the mod author: " + e);
            }
        }

        private static byte[] Obfuscate(byte[] data, string key)
        {
            byte[] obfuscatedData = new byte[data.Length];
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Perform bitwise XOR operation with the key
            for (int i = 0; i < data.Length; i++)
            {
                obfuscatedData[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return obfuscatedData;
        }

        // Pulls all variables from the savefile and puts them into SaveData so the information can be accessed easily without caching in any scripts
        public static void Load(float saveID)
        {
            try
            {
                byte[] encryptedData;

                using (FileStream fileStream = new FileStream(savePath, FileMode.Open))
                {
                    // Read encrypted data from file
                    encryptedData = new byte[fileStream.Length];
                    fileStream.Read(encryptedData, 0, (int)fileStream.Length);
                }

                // Decrypt the data based on player's saveID
                encryptedData = Obfuscate(encryptedData, saveID.ToString());

                // Read decrypted data
                using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                {
                    using (BinaryReader reader = new BinaryReader(memoryStream))
                    {
                        // Save ID unique to all saves
                        float saveidentifier = reader.ReadSingle();
                        if (saveID != saveidentifier) return;

                        int numProperties = reader.ReadInt32();

                        for (int i = 0; i < numProperties; i++)
                        {
                            string itemName = reader.ReadString();

                            FieldInfo field = typeof(SaveData).GetField(itemName);
                            PropertyInfo property = typeof(SaveData).GetProperty(itemName);

                            if (field != null)
                                trySetField(field, reader);
                            else
                            {
                                ModConsole.LogError("Field not found, report this to the mod author: " + itemName);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModConsole.LogError("Failed to load! Report this to the mod author: " + e);
            }
        }
    }
}
