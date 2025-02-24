using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RandomMazeSceneManager : MonoBehaviour
{
    public static bool isPaused = false;
    public int mazeSize = 11;
    public int numPeople = 2;
    public int numObjects = 10;
    public int lightingLevel = 1;

    public bool autoIdentifyHumans = true;


    private MazeGenerator mazeGenerator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // LoadVariablesFromPlayerPrefs();
        InitiateMazeGenerator();
        // UpdateCameraCapture();
    }   

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q)){
            ResetScene();
        }
        if(Input.GetKeyDown(KeyCode.P)){
            PausePhysics();
        }
    }

    void PausePhysics(){
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
    }

    void LoadVariablesFromPlayerPrefs(){
        mazeSize = PlayerPrefs.GetInt("MazeSize");
        lightingLevel = PlayerPrefs.GetInt("Lighting");
        int val = PlayerPrefs.GetInt("DetectHumans");
        if(val == 0){
            autoIdentifyHumans = false;
        }else{
            autoIdentifyHumans = true;
        }
    }

    void InitiateMazeGenerator(){
        MazeGenerator mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        if(mazeGenerator != null){
            mazeGenerator.lightingLevel = lightingLevel;
            mazeGenerator.mazeHeight = mazeSize;
            mazeGenerator.mazeWidth = mazeSize;
            mazeGenerator.pointCount = numPeople;

            Debug.Log(mazeGenerator.pointCount);
            

            // Instantiate(MazeGenerator);

            mazeGenerator.GenerateMaze();
        }
        
        

    }

    void UpdateCameraCapture(){
        WebSocketCameraCapture cameraCapture = FindFirstObjectByType<WebSocketCameraCapture>();
        if(cameraCapture != null){
            Debug.Log("Camera capture: "+ autoIdentifyHumans);
            cameraCapture.autoIdentifyHumans = autoIdentifyHumans;
        }
    }


    void ResetScene(){
        SceneManager.LoadSceneAsync("RandomMaze");
    }
}