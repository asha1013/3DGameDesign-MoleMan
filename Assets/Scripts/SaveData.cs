using System;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // Settings
    public float musicVolume = 1f;
    public float SFXVolume = 1f;
    public float mouseSensitivity = 0.5f;

    // Game Data
    public bool myteUnlocked;
    public bool gnomeUnlocked;
    public bool flyUnlocked;
    public bool frogUnlocked;
//  public bool knightUnlocked;
    public int myteProgress;
    public int gnomeProgress;
    public int flyProgress;
    public int frogProgress;
//  public int knightProgress;
    public int playerMaxHP;
//  public int playerStamina;
//  public int playerDamageMultiplier;
//  public bool tutorialDone;
    public bool seeClearly = false;
    public string equippedWeaponName;
    public int totalWins;
    public int totalDeaths;
    public string[] persistentInventoryItemNames;
    public int[] persistentInventoryQuantities;

    // Mushroom garden data (4 plots)
    public string[] plotMushroomNames = new string[4];
    public int[] plotProgress = new int[4];
    public bool[] plotCompleted = new bool[4];
}
