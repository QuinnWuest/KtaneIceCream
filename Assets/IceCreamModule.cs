﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IceCream;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

public class IceCreamModule : MonoBehaviour
{
    // Bomb and Module
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMModSettings ModSettings;

    // Buttons
    public KMSelectable LeftButton;
    public KMSelectable RightButton;
    public KMSelectable SellButton;

    // Labels
    public TextMesh CustomerLabel;
    public TextMesh FlavourLabel;

    // Indicators
    public GameObject[] Scoops;
    private Vector3[] ScoopPositions;
    public Material[] FlavourMaterials;
    public GameObject OpenSign;

    // Module Identification (for logging)
    private int _moduleID;
    private static int _moduleIDCounter = 1;
    private bool _isSolved;
    private bool _isActivated;

    // Mod Settings (opening times)
    class Settings
    {
        public bool? openingTimeEnabled = null;
        public bool? hoursCommandEnabled = null;

        public bool OpeningTimeEnabled() { return openingTimeEnabled ?? true; }
        public bool HoursCommandEnabled() { return hoursCommandEnabled ?? true; }
    }
    Settings modSettings;

    // Flavour Definitions
    class Flavour
    {
        public string Name;
        public Allergies[] Allergies;

        public Flavour(string name, params Allergies[] alg)
        {
            Name = name;
            Allergies = alg;
        }
    }
    Flavour[] Flavours = new Flavour[] {
        new Flavour("Tutti Frutti", Allergies.Fruit, Allergies.Raspberry, Allergies.Cherry, Allergies.Strawberry), // 0
        new Flavour("Rocky Road", Allergies.Chocolate, Allergies.Nuts, Allergies.Marshmallows), // 1
        new Flavour("Raspberry Ripple", Allergies.Raspberry, Allergies.Fruit), // 2
        new Flavour("Double Chocolate", Allergies.Chocolate), // 3
        new Flavour("Double Strawberry", Allergies.Strawberry, Allergies.Fruit), // 4
        new Flavour("Cookies and Cream", Allergies.Cookies), // 5
        new Flavour("Neapolitan", Allergies.Strawberry, Allergies.Chocolate, Allergies.Fruit), // 6
        new Flavour("Mint Chocolate Chip", Allergies.Chocolate, Allergies.Mint), // 7
        new Flavour("The Classic", Allergies.Chocolate, Allergies.Cherry, Allergies.Fruit), // 8
        new Flavour("Vanilla") // 9
    };
    int[][] FlavourLists = new int[][] {
        new int[] {5, 6, 0, 8, 1, 3, 7, 4, 2, 9}, // More LIT than UNLIT indicators.
        new int[] {3, 7, 6, 1, 0, 4, 5, 2, 8, 9}, // Empty port plate.
        new int[] {6, 0, 5, 2, 4, 7, 3, 8, 1, 9}, // 3 or more batteries.
        new int[] {4, 5, 1, 8, 6, 3, 0, 2, 7, 9}  // Otherwise
    };

    // Allergy Definitions
    enum Allergies
    {
        Chocolate,
        Strawberry,
        Raspberry,
        Nuts,
        Cookies,
        Mint,
        Fruit,
        Cherry,
        Marshmallows
    }

    // Customer Definitions
    string[] CustomerNames = new string[] { "Mike", "Tim", "Tom", "Dave", "Adam", "Cheryl", "Sean", "Ashley", "Jessica", "Taylor", "Simon", "Sally", "Jade", "Sam", "Gary", "Victor", "George", "Jacob", "Pat", "Bob" };
    int[,,] AllergyTable = new int[,,] {
        { {1, 5, 0}, {6, 8, 3}, {0, 7, 1}, {4, 3, 2}, {3, 6, 1} }, // Mike
        { {0, 8, 3}, {2, 1, 4}, {4, 3, 5}, {2, 6, 7}, {1, 4, 3} }, // Tim
        { {8, 4, 5}, {1, 6, 7}, {2, 5, 6}, {3, 7, 5}, {3, 6, 1} }, // Tom
        { {2, 6, 7}, {0, 1, 4}, {8, 2, 3}, {7, 8, 1}, {5, 7, 3} }, // Dave
        { {3, 4, 1}, {3, 6, 2}, {0, 2, 1}, {2, 4, 7}, {8, 5, 6} }, // Adam
        { {1, 6, 3}, {7, 5, 2}, {1, 4, 5}, {4, 2, 0}, {3, 7, 5} }, // Cheryl
        { {4, 6, 1}, {2, 3, 6}, {1, 5, 7}, {6, 8, 2}, {2, 7, 4} }, // Sean
        { {6, 2, 5}, {4, 1, 7}, {0, 8, 2}, {1, 2, 6}, {3, 6, 7} }, // Ashley
        { {4, 2, 6}, {1, 2, 3}, {0, 3, 4}, {6, 5, 0}, {4, 7, 8} }, // Jessica
        { {6, 3, 5}, {5, 1, 2}, {4, 2, 6}, {7, 1, 0}, {3, 7, 2} }, // Taylor
        { {0, 3, 5}, {1, 6, 4}, {5, 4, 8}, {2, 0, 7}, {7, 3, 6} }, // Simon
        { {4, 6, 3}, {1, 0, 2}, {6, 7, 4}, {2, 5, 8}, {0, 3, 1} }, // Sally
        { {3, 7, 1}, {0, 8, 2}, {7, 1, 3}, {6, 7, 8}, {4, 5, 1} }, // Jade
        { {2, 4, 1}, {7, 8, 0}, {3, 4, 6}, {1, 0, 3}, {6, 5, 2} }, // Sam
        { {1, 2, 5}, {6, 8, 0}, {3, 2, 1}, {7, 4, 5}, {1, 8, 4} }, // Gary
        { {0, 3, 1}, {2, 5, 7}, {3, 4, 6}, {6, 7, 1}, {5, 3, 0} }, // Victor
        { {8, 1, 2}, {6, 4, 8}, {0, 4, 3}, {1, 6, 4}, {3, 2, 5} }, // George
        { {7, 3, 2}, {1, 5, 6}, {5, 4, 7}, {3, 4, 0}, {6, 2, 1} }, // Jacob
        { {5, 6, 2}, {1, 3, 6}, {3, 4, 7}, {2, 0, 5}, {8, 1, 3} }, // Pat
        { {5, 6, 8}, {2, 1, 0}, {4, 8, 2}, {4, 2, 5}, {0, 5, 1} }  // Bob
    };

    // Input Tracking
    int CurrentFlavor = 0;
    int[][] FlavorOptions;
    int CurrentStage = 0;
    int MaxStages = 3;

    // Conditional Tallies
    bool HasEmptyPortPlate;
    int LastDigit;

    // Solutions
    int[] CustomerNamesSolution;
    int[] Solution;
    int[] CorrectFlavors;

    void Start()
    {
        _moduleID = _moduleIDCounter++;

        CustomerLabel.text = "";
        FlavourLabel.text = "ICE CREAM";

        ModSettings.RefreshSettings();
        modSettings = JsonConvert.DeserializeObject<Settings>(ModSettings.Settings);

        if (!modSettings.openingTimeEnabled.HasValue || !modSettings.hoursCommandEnabled.HasValue)
        {
            modSettings.openingTimeEnabled = modSettings.OpeningTimeEnabled();
            modSettings.hoursCommandEnabled = modSettings.HoursCommandEnabled();
            try
            {
                File.WriteAllText(ModSettings.SettingsPath, JsonConvert.SerializeObject(modSettings, Formatting.Indented));
            }
            catch
            {
                //
            }
        }

        OpenSign.SetActive(modSettings != null && !modSettings.OpeningTimeEnabled());

        CorrectFlavors = new int[MaxStages];

        LeftButton.OnInteract += delegate { HandlePress(-1); return false; };
        RightButton.OnInteract += delegate { HandlePress(1); return false; };
        SellButton.OnInteract += delegate { HandlePress(0); return false; };

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        ScoopPositions = new Vector3[Scoops.Length];
        for (int i = 0; i < Scoops.Length; i++)
        {
            ScoopPositions[i] = Scoops[i].transform.localPosition;
            Scoops[i].SetActive(false);
        }
    }

    void OnActivate()
    {
        _isActivated = true;
        // Conditional tally lookup.
        foreach (object[] plate in BombInfo.GetPortPlates())
        {
            if (plate.Length == 0)
            {
                HasEmptyPortPlate = true;
                break;
            }
        }
        foreach (int digit in BombInfo.GetSerialNumberNumbers())
        {
            LastDigit = digit;
        }

        GenerateSolutions();
        LogCurrentStage();
        UpdateDisplays();
    }

    void GenerateSolutions()
    {
        int[] correctList = null;

        CustomerNamesSolution = new int[MaxStages];
        Solution = new int[MaxStages];
        FlavorOptions = new int[MaxStages][];

        // Choose correct flavour list if not done already.
        if (correctList == null)
        {
            if (BombInfo.GetOnIndicators().Count() > BombInfo.GetOffIndicators().Count()) { correctList = FlavourLists[0]; }
            else if (HasEmptyPortPlate) { correctList = FlavourLists[1]; }
            else if (BombInfo.GetBatteryCount() >= 3) { correctList = FlavourLists[2]; }
            else { correctList = FlavourLists[3]; }
        }

        // Generate solution per stage.
        for (int i = 0; i < MaxStages; i++)
        {
            CustomerNamesSolution[i] = -1;
            int customerID = -1;
            int[] stageFlavours = new int[5];

            for (int j = 0; j < 5; j++)
                stageFlavours[j] = -1;

            int flavourId = -1;

            // Create list of selectable flavours for the stage.
            for (int si = 0; si < 4; si++)
            {
                stageFlavours[si] = -1;
                while (Array.Exists(stageFlavours, x => x == flavourId))
                    flavourId = Random.Range(0, Flavours.Length - 1);
                stageFlavours[si] = flavourId;
            }
            stageFlavours[4] = 9;

            // Choose a customer for the stage.
            while (Array.Exists(CustomerNamesSolution, x => x == customerID))
                customerID = Random.Range(0, CustomerNames.Length);

            // Determine if the customer is allergic to any of the chosen flavours.
            bool[] bad = new bool[stageFlavours.Length];
            for (int j = 0; j < 3; j++)
            {
                int allergy = AllergyTable[customerID, LastDigit / 2, j];
                for (int k = 0; k < stageFlavours.Length; k++)
                {
                    if (Flavours[stageFlavours[k]].Allergies.Contains((Allergies) allergy) && !bad[k])
                    {
                        bad[k] = true;
                    }
                }
            }

            // Determine which acceptable flavour comes first in the flavour list.
            int lowestNum = 9;
            int sol = 4;
            for (int l = 0; l < stageFlavours.Length; l++)
            {
                if (!bad[l])
                {
                    if (Array.IndexOf(correctList, stageFlavours[l]) < lowestNum)
                    {
                        lowestNum = Array.IndexOf(correctList, stageFlavours[l]);
                        sol = l;
                    }
                }
            }

            // Record solution.
            Solution[i] = sol;
            CustomerNamesSolution[i] = customerID;
            FlavorOptions[i] = stageFlavours;

            //Debug.LogFormat("[Ice Cream #{0}] Stage {1} Flavour Options: '{2}', '{3}', '{4}', '{5}', '{6}'", moduleId, i + 1, Flavours[flavourOptions[i][0]].name, Flavours[flavourOptions[i][1]].name, Flavours[flavourOptions[i][2]].name, Flavours[flavourOptions[i][3]].name, Flavours[flavourOptions[i][4]].name);
        }

        //Debug.LogFormat("[Ice Cream #{0}] Solution: '{1}', '{2}', '{3}'", moduleId, Flavours[flavourOptions[0][solution[0]]].name, Flavours[flavourOptions[1][solution[1]]].name, Flavours[flavourOptions[2][solution[2]]].name);
        //Debug.LogFormat("[Ice Cream #{0}] Customers: '{1}', '{2}', '{3}'", moduleId, CustomerNames[solCustomerNames[0]], CustomerNames[solCustomerNames[1]], CustomerNames[solCustomerNames[2]]);
    }

    void LogCurrentStage()
    {
        Debug.LogFormat("[Ice Cream #{0}] Stage {1}\nCustomer: {2}\nFlavour Options: {3}, {4}, {5}, {6}, {7}\nSolution: {8}",
            _moduleID, CurrentStage + 1, CustomerNames[CustomerNamesSolution[CurrentStage]],
            Flavours[FlavorOptions[CurrentStage][0]].Name, Flavours[FlavorOptions[CurrentStage][1]].Name, Flavours[FlavorOptions[CurrentStage][2]].Name, Flavours[FlavorOptions[CurrentStage][3]].Name, Flavours[FlavorOptions[CurrentStage][4]].Name,
            Flavours[FlavorOptions[CurrentStage][Solution[CurrentStage]]].Name);
    }

    void UpdateDisplays()
    {
        CustomerLabel.text = CustomerNames[CustomerNamesSolution[CurrentStage]];
        FlavourLabel.text = Flavours[FlavorOptions[CurrentStage][CurrentFlavor]].Name;
    }

    void HandlePress(int button)
    {
        Audio.PlaySoundAtTransform("tick", transform);
        if (!_isActivated)
            return;
        switch (button)
        {
            case -1:
                RotateFlavours(-1);
                LeftButton.AddInteractionPunch();
                break;
            case 1:
                RotateFlavours(1);
                RightButton.AddInteractionPunch();
                break;
            case 0:
                Submit();
                SellButton.AddInteractionPunch();
                break;
            default:
                BombModule.HandleStrike();
                break;
        }
    }

    void RotateFlavours(int dir)
    {
        // Choose next flavour to display.
        CurrentFlavor += dir;
        if (CurrentFlavor < 0) CurrentFlavor += FlavorOptions[CurrentStage].Length;
        else if (CurrentFlavor >= FlavorOptions[CurrentStage].Length) CurrentFlavor -= FlavorOptions[CurrentStage].Length;

        UpdateDisplays();
    }

    void Submit()
    {
        // Check if submitted on an even minute.
        if (!modSettings.OpeningTimeEnabled() || (int) (BombInfo.GetTime() / 60) % 2 == 0)
        {
            if (CurrentStage < MaxStages && CurrentFlavor == Solution[CurrentStage])
            {
                //Debug.LogFormat("[Ice Cream #{0}] Flavour '{1}' for customer '{2}' submitted correctly.", moduleId, Flavours[flavourOptions[currentStage][currentFlavour]].name, CustomerNames[solCustomerNames[currentStage]]);
                Debug.LogFormat("[Ice Cream #{0}] {1} is correct.", _moduleID, Flavours[FlavorOptions[CurrentStage][CurrentFlavor]].Name);
                CorrectFlavors[CurrentStage] = FlavorOptions[CurrentStage][CurrentFlavor];
                Scoops[CurrentStage].GetComponent<MeshRenderer>().sharedMaterial = FlavourMaterials[FlavorOptions[CurrentStage][CurrentFlavor]];
                CurrentStage++;
                if (CurrentStage >= MaxStages)
                {
                    BombModule.HandlePass();
                    _isSolved = true;
                }
                else
                {
                    LogCurrentStage();
                    UpdateDisplays();
                }
            }
            else
            {
                if (CurrentStage < MaxStages)
                {
                    //Debug.LogFormat("[Ice Cream #{0}] Flavour '{1}' for customer '{2}' submitted incorrectly.", moduleId, Flavours[flavourOptions[currentStage][currentFlavour]].name, CustomerNames[solCustomerNames[currentStage]]);
                    Debug.LogFormat("[Ice Cream #{0}] {1} is incorrect.", _moduleID, Flavours[FlavorOptions[CurrentStage][CurrentFlavor]].Name);
                    //currentStage = 0;
                    BombModule.HandleStrike();
                    GenerateSolutions();
                    LogCurrentStage();
                    UpdateDisplays();
                }
            }
        }
        else
        {
            if (CurrentStage < MaxStages)
            {
                ///Debug.LogFormat("[Ice Cream #{0}] Flavour '{1}' for customer '{2}' submitted while parlour is closed.", moduleId, Flavours[flavourOptions[currentStage][currentFlavour]].name, CustomerNames[solCustomerNames[currentStage]]);
                Debug.LogFormat("[Ice Cream #{0}] {1} submitted when parlour is closed.", _moduleID, Flavours[FlavorOptions[CurrentStage][CurrentFlavor]].Name);
                //currentStage = 0;
                BombModule.HandleStrike();
                GenerateSolutions();
                LogCurrentStage();
                UpdateDisplays();
            }
        }

        // Update stage indicator.
        for (int i = 0; i < 3; i++)
            if (Scoops[i].activeSelf != CurrentStage > i)
                StartCoroutine(ScoopAnimation(i, CurrentStage > i));
    }

    private IEnumerator ScoopAnimation(int ix, bool activate)
    {
        var offset = ScoopPositions[ix] + new Vector3(Random.Range(0f, 10f) - 5f, 10f, Random.Range(0f, 6f) - 3f);
        var duration = .925f;
        var elapsed = 0f;
        if (activate)
        {
            Scoops[ix].SetActive(true);
            Scoops[ix].transform.localRotation = Quaternion.Euler(Random.Range(0, 360f), Random.Range(0, 360f), Random.Range(0, 360f));
            while (elapsed < duration)
            {
                Scoops[ix].transform.localPosition = Vector3.Lerp(offset, ScoopPositions[ix], elapsed / duration);
                yield return null;
                elapsed += Time.deltaTime;
            }
            Scoops[ix].transform.localPosition = ScoopPositions[ix];
        }
        else
        {
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Scoops[ix].transform.localPosition = Vector3.Lerp(ScoopPositions[ix], offset, elapsed / duration);
            }
            Scoops[ix].SetActive(false);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} left | !{0} right | !{0} cycle | !{0} sell [sells currently selected flavour] | !{0} sell cookies [sells Cookies & Cream] | !{0} hours [check opening hours]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (!Regex.IsMatch(command, "^(cycle|left|right|l|r|sell|submit|middle|s|m|hours)( .*)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            yield break;

        string[] pieces = command.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (pieces.Length > 1 && (pieces[0] == "submit" || pieces[0] == "sell" || pieces[0] == "middle" || pieces[0] == "s" || pieces[0] == "m"))
        {
            command = command.Substring(pieces[0].Length).Trim();
            if (Flavours.All(x => !x.Name.StartsWith(command, StringComparison.InvariantCultureIgnoreCase)))
            {
                yield return string.Format("sendtochaterror I don't know of a flavour called {0}", command);
                yield break;
            }

            yield return null;
            string originalLabel = FlavourLabel.text;
            do
            {
                if (FlavourLabel.text.StartsWith(command, StringComparison.InvariantCultureIgnoreCase))
                {
                    SellButton.OnInteract();
                    yield break;
                }
                RightButton.OnInteract();
                yield return new WaitForSeconds(0.25f);
            }
            while (FlavourLabel.text != originalLabel);
            yield return "unsubmittablepenalty";
            yield break;
        }
        else if (pieces.Length == 1 && pieces[0] == "cycle")
        {
            yield return null;
            string originalLabel = FlavourLabel.text;
            do
            {
                RightButton.OnInteract();
                yield return new WaitForSeconds(1.5f);
            }
            while (FlavourLabel.text != originalLabel);
            yield break;
        }
        else if (pieces.Length == 1 && pieces[0] == "hours")
        {
            if (modSettings.HoursCommandEnabled())
                yield return "sendtochat " + (modSettings.OpeningTimeEnabled() ? "We are open every other hour today." : "We're open all day today!");
            else
                yield return "sendtochat Sorry, hours are currently unavailable. Enjoy your ice cream!";
            yield break;
        }

        var buttons = new List<KMSelectable>();

        for (int i = 0; i < pieces.Length; i++)
        {
            switch (pieces[i])
            {
                case "l":
                case "left":
                    buttons.Add(LeftButton);
                    break;

                case "r":
                case "right":
                    buttons.Add(RightButton);
                    break;

                case "s":
                case "m":
                case "sell":
                case "submit":
                case "middle":
                    buttons.Add(SellButton);
                    break;

                default:
                    yield break;
            }
        }

        if (buttons.Count > 0)
        {
            yield return null;
            foreach (var button in buttons)
            {
                button.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            while (CurrentFlavor != Solution[CurrentStage])
            {
                RightButton.OnInteract();
                yield return new WaitForSeconds(.1f);
            }

            if (modSettings.OpeningTimeEnabled())
                while ((int) (BombInfo.GetTime() / 60) % 2 != 0)
                    yield return true;

            SellButton.OnInteract();
            yield return new WaitForSeconds(.25f);
        }
    }
}
