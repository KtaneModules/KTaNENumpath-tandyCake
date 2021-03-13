using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class NumpathScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;

    public KMSelectable[] buttons;
    public TextMesh screen;
    public TextMesh colorblindText;

    private Color[] textColors = new Color[] {
    new Color (1, 0, 0), //Red
    new Color (0, 1, 0), //Green
    new Color (0, 0, 1), //Blue
    new Color (1, 1, 0), //Yellow
    new Color (0.6f, 0, 0.6f), //Purple
    new Color (1, 0.6f, 0) //Orange
    };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    bool colorblindOn = false;

    int[][,] grids = new int[][,]
    {
        new int[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } },
        new int[,] { { 1, 8, 7 }, { 2, 9, 6 }, { 3, 4, 5 } },
        new int[,] { { 9, 8, 7 }, { 6, 5, 4 }, { 3, 2, 1 } },
        new int[,] { { 2, 1, 4 }, { 3, 6, 5 }, { 8, 7, 9 } },
        new int[,] { { 1, 8, 6 }, { 4, 2, 9 }, { 7, 5, 3 } },
        new int[,] { { 1, 4, 7 }, { 2, 5, 8 }, { 3, 6, 9 } },
    };
    int[,] chosenGrid;

    string[] colorNames = new string[] { "Red", "Green", "Blue", "Yellow", "Purple", "Orange" };
    string[] directionNames = new string[] { "up", "left", "down", "right" };
    char[] letterNames = new char[] { 'U', 'L', 'D', 'R' };
    int colorIndex;
    int stage = 0;
    int startNum;
    int starti, startj;
    int i, j;
    List<int> solution = new List<int>();

    void Awake () {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons) 
        {
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        }
        foreach (KMSelectable button in buttons)
        {
            button.OnInteractEnded += delegate () { Release(button); };
        }
    }

    void Start ()
    {
        if (Colorblind.ColorblindModeActive) colorblindOn = true;
        GetNumbers();
        GetColors();
        GetStart();
        DoLogging();
    }

    void ButtonPress(KMSelectable button)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch(0.5f);
        if (moduleSolved)
        {
            return;
        }
        switch (Array.IndexOf(buttons, button))
        {
            case 0: if (i == 0) Strike(0); else i -= 1; break;
            case 1: if (j == 0) Strike(1); else j -= 1; break;
            case 2: if (i == 2) Strike(2); else i += 1; break;
            case 3: if (j == 2) Strike(3); else j += 1; break;
        }
        if (chosenGrid[i,j] == solution[stage])
        {
            stage++;
            Debug.LogFormat("[Numpath #{0}] Submitted {1}.", moduleId, chosenGrid[i, j]);
            if (stage == solution.Count)
            {
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                screen.text = string.Empty;
                colorblindText.text = string.Empty;
            }
        }
    }

    void Release(KMSelectable button)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, button.transform);
    }

    void GetNumbers()
    {
        List<int> SNN = Bomb.GetSerialNumberNumbers().ToList();
        int prev = -1;
        for (int i = 0; i < SNN.Count; i++)
        {
            int temp = (SNN[i] == 0) ? 1 : SNN[i];
            if (prev != temp)
            {
                solution.Add(temp);
                prev = temp;
            }
        }
    }
    void GetColors()
    {
        colorIndex = UnityEngine.Random.Range(0, 6);
        screen.color = textColors[colorIndex];
        colorblindText.color = textColors[colorIndex];
        SetCB();
        chosenGrid = grids[colorIndex];
    }
    void GetStart()
    {
        starti = UnityEngine.Random.Range(0, 3);
        startj = UnityEngine.Random.Range(0, 3);
        while (solution.Contains(chosenGrid[starti,startj]))
        {
            starti = UnityEngine.Random.Range(0, 3);
            startj = UnityEngine.Random.Range(0, 3);
        }
        i = starti;
        j = startj;
        screen.text = chosenGrid[starti, startj].ToString();
    }
    void DoLogging()
    {
        Debug.LogFormat("[Numpath #{0}] The display bears a {1} {2}.", moduleId, colorNames[colorIndex], chosenGrid[starti,startj]);
        Debug.LogFormat("[Numpath #{0}] The numbers you must submit are {1}.", moduleId, solution.Join(" "));
    }

    void Strike(int buttonPressed)
    {
        Debug.LogFormat("[Numpath #{0}] You tried moving {1} from {2}, module reset.", moduleId, directionNames[buttonPressed], chosenGrid[i, j]);
        GetComponent<KMBombModule>().HandleStrike();
        i = starti;
        j = startj;
    }

    void SetCB()
    {
        if (colorblindOn) colorblindText.text = colorNames[colorIndex]; else colorblindText.text = string.Empty;
    }

    int iGet(int find)
    {
        for (int jloc = 0; jloc < 3; jloc++)
        {
            for (int iloc = 0; iloc < 3; iloc++)
            {
                if (chosenGrid[iloc, jloc] == find) return iloc;
            }
        }
        return -1;
    }
    int jGet(int find)
    {
        for (int jloc = 0; jloc < 3; jloc++)
        {
            for (int iloc = 0; iloc < 3; iloc++)
            {
                if (chosenGrid[iloc, jloc] == find) return jloc;
            }
        }
        return -1;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} move ULDR to move in those directions. Use !{0} colorblind to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command)
    {
        string[] parameters = Command.Trim().ToUpperInvariant().Split(' ');
        if (Regex.IsMatch(Command, @"\s*colorblind\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            colorblindOn = !colorblindOn;
            SetCB();
        }
        if (Regex.IsMatch(Command, @"\s*move\s*[ULDR]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            foreach (char letter in parameters[1])
            {
                buttons[Array.IndexOf(letterNames, letter)].OnInteract();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!moduleSolved)
        {
            if (iGet(solution[stage]) < i) { buttons[0].OnInteract(); yield return new WaitForSecondsRealtime(0.1f); if (moduleSolved) break; }
            if (jGet(solution[stage]) < j) { buttons[1].OnInteract(); yield return new WaitForSecondsRealtime(0.1f); if (moduleSolved) break; }
            if (iGet(solution[stage]) > i) { buttons[2].OnInteract(); yield return new WaitForSecondsRealtime(0.1f); if (moduleSolved) break; }
            if (jGet(solution[stage]) > j) { buttons[3].OnInteract(); yield return new WaitForSecondsRealtime(0.1f); if (moduleSolved) break; }
        }
    }
}
