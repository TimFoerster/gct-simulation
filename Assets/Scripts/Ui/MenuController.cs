using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(ScenarioSeed), typeof(SimulationRandomNumberGenerator))]
public class MenuController : MonoBehaviour
{
    [SerializeField]
    TMP_InputField seed;


    [SerializeField]
    GameObject loadingScreen;

    [SerializeField]
    GameObject mainMenu;

    [SerializeField]
    Slider progressBar;

    [SerializeField]
    TMP_Text levelName;

    [SerializeField]
    TMP_InputField appInterval;

    [SerializeField]
    TMP_InputField broadcastInterval;

    [SerializeField]
    TMP_InputField receiveAccuracy;

    [SerializeField]
    TMP_Dropdown algorithmsDropdown;

    [SerializeField] TMP_Text[] peoplePreview;

    ScenarioSeed scenarioSeed;
    SimulationRandomNumberGenerator simRng;

    SimulationSettings simulationSettings;

    System.Type[] simulations = new System.Type[6]
    {
        null, 
        typeof(TrainSimulation), 
        typeof(RestaurantSimulation), 
        typeof(OfficeSimulation), 
        typeof(ConferenceSimulation),
        typeof(ParkSimulation)
    };

    private void OnEnable()
    {
        simulationSettings = SimulationSettings.Instance;
        simRng = GetComponent<SimulationRandomNumberGenerator>();
        scenarioSeed = GetComponent<ScenarioSeed>();
    }
    void Start()
    {
        if (simulationSettings.Seed == default)
        {
            simulationSettings.SetSeed(GenerateSeed());
        }
        seed.text = simulationSettings.Seed.ToString();
        if (simulationSettings.simulationIndex > 0)
        {
            if (!Scenario(simulationSettings.simulationIndex))
            {
                Debug.Log("Invalid Scenario, abort...");
                Exit();
            }
            return;
        }

        if (simulationSettings.IsHeadless)
        {
            Debug.Log("No Scenario supplied, choosing random one");
            Scenario(Random.Range(1, 5));
            return;
        }

        appInterval.text = simulationSettings.AppUpdateInterval.ToString(CultureInfo.InvariantCulture);
        broadcastInterval.text = simulationSettings.BroadcastInterval.ToString(CultureInfo.InvariantCulture);
        algorithmsDropdown.options.Clear();
        algorithmsDropdown.options = simulationSettings.algorithms.Select(a => new TMP_Dropdown.OptionData(a)).ToList();


        
    }

    public void Update()
    {
        /*
        if (personCount != 714)
            RandomSeed();
        */
    }

    public void OnSeedChange(string s)
    {
        if (int.TryParse(s, out int seed))
        {
            Preview(seed);
        }
        
    }

    protected void Preview(int seed)
    {
        for(int i = 1; i < peoplePreview.Length; i++)
        {
            scenarioSeed.Init(seed);
            simRng.Reset();
            peoplePreview[i].text = simulations[i].GetMethod("GetPersonCount").Invoke(null, new object[1] { simRng }).ToString();
        }
    }

    int GenerateSeed()
    {
        return Random.Range(int.MinValue, int.MaxValue);
    }

    public void RandomSeed()
    {
        seed.text = GenerateSeed().ToString();
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }

    public void OnScenarioClick(int index)
    {
        Scenario(index);
    }

    bool Scenario(int index)
    {         
        if (index < 1 || index > 5)
        {
            return false;
        }
        simulationSettings.SetSeed(int.Parse(seed.text));

        var names = new string[] { "Main Menu", "Public Transport", "Restaurant", "Office", "Conference", "Park" };
        levelName.text = "Loading " + names[index] + " ...";

        Debug.Log("Loading " + names[index] + " with Seed " + simulationSettings.Seed);

        StartCoroutine(LoadLevel(index));

        return true;
    }

    IEnumerator LoadLevel(int index)
    {

        loadingScreen.SetActive(true);
        mainMenu.SetActive(false);

        var op = SceneManager.LoadSceneAsync(index);
        while(!op.isDone)
        {
            progressBar.value = Mathf.Clamp01(op.progress / .9f);
            yield return null;
        }

    }

    public void OnAppIntervalChange()
    {
        simulationSettings.SetAppUpdateInterval(float.Parse(appInterval.text));
    }

    public void OnBroadcastIntervalChange()
    {
        simulationSettings.SetBroadcastInterval(float.Parse(broadcastInterval.text));
    }
    public void OnReceiveAccuracyChange()
    {
        if (receiveAccuracy.text.Length > 0)
            simulationSettings.SetReceiveAccuracy(float.Parse(receiveAccuracy.text) / 100f);
    }

    public void OnAlgorithmChange()
    {
        simulationSettings.SetAlgorithm(algorithmsDropdown.value);
    }

}
