using System;
using System.IO;
using System.Net;
using UnityEngine;

public class FFMpegInit : MonoBehaviour
{
    private void Start()
    {
        Application.logMessageReceived += ApplicationOnlogMessageReceived;

        File.Delete(Application.persistentDataPath + "/log.txt");
        File.WriteAllText(Application.persistentDataPath + "/log.txt", "Hello World!");

        try
        {
            Debug.Log(Application.persistentDataPath + "/testout.mp4");
            FFMpegKitInterop.Init();
            FFMpegKitInterop.StreamToFile(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9943), Application.persistentDataPath + "/testout.mp4");
        }
        catch (Exception e)
        {
            string error = e.ToString();
            Debug.Log("ERR: " + error);
        }
    }

    private void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
    {
        using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/log.txt", true);
        sw.WriteLine($"[{type}] | {condition}");
    }
}