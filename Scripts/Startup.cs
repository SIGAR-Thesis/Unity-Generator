﻿using System;
using Simulation;
using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.PlayerLoop;
using System.IO;

public class Startup : MonoBehaviour
{
    // Start is called before the first frame update
    static bool _screenshotCheck;
    private static string outputPath = "datasets/";
    private static string imagePrefix = "IMG";
    private static int currImg;
    private static string jobId;


    IEnumerator Start()
    {
        Initialization();
        var config = LoadJson();

        Debug.Log(JsonUtility.ToJson(config));
        for (int i = 0; i < config.images; i++)
        {
            //Start of repeatable execution cycle
            config.BuildScene();
            // DynamicGI.UpdateEnvironment();
            StartCoroutine("CaptureScreen");
            yield return new WaitUntil(() => _screenshotCheck);
            _screenshotCheck = false;
        }

        Console.WriteLine("OK");
    }

    static void Initialization()
    {
        // Check for the output directory
        if (!System.IO.Directory.Exists(outputPath))
            System.IO.Directory.CreateDirectory(outputPath);
        string[] arguments = System.Environment.GetCommandLineArgs();
        foreach (var item in arguments)
        {
            if (item.StartsWith("--jobId"))
            {
                jobId = item.Substring(8);

                if (!System.IO.Directory.Exists(System.IO.Path.Combine(outputPath, jobId)))
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(outputPath, jobId));
            }
        }
    }

    IEnumerator CaptureScreen()
    {
        yield return new WaitForEndOfFrame();

        var screenshotName = imagePrefix + "-" + currImg++ + ".png";
        // TODO: Discvoer why CaptureScreenshot saves in MS1_Data. This is a quick fix to get out of the MS1_Data folder
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine("..", "..", outputPath, jobId, screenshotName));

        var objects = FindObjectsOfType<GameObject>();
        foreach (var g in objects)
        {
            if (g.name != "root")
                Destroy(g.gameObject);
        }

        _screenshotCheck = true;
    }

    static SceneDefinition LoadJson()
    {
         const string path = "Config/config";
         var targetFile = Resources.Load<TextAsset>(path);
         var config = ScriptableObject.CreateInstance<SceneDefinition>();
         config.Load(targetFile.text);
         return config;
        
        /*string path = System.IO.Path.Combine("build", "Unity", "Config", jobId, "config.json");
        Console.WriteLine("Here config");
        Console.WriteLine(path);
        using (StreamReader r = new StreamReader(path))
        {
            string jsonText = r.ReadToEnd();
            //SceneDefinition config = JsonUtility.FromJson<SceneDefinition>(json);

            var config = ScriptableObject.CreateInstance<SceneDefinition>();
            config.Load(jsonText);
            return config;
        }*/
    }

    // Update is called once per frame
    void Update()
    {
    }
}