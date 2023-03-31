using UnityEngine;



public class Logger
{

    private static Logger instance = null;

    private GameTime gameTime;

    public static Logger Instance => instance;
    private SimulationLogger simulationLogger;

    public static Logger Init(GameTime gameTime, SimulationLogger simulationLogger)
    {
        instance = new Logger(gameTime, simulationLogger);
        return instance;
    }

    private Logger(GameTime gameTime, SimulationLogger simulationLogger)
    {
        this.gameTime = gameTime;
        this.simulationLogger = simulationLogger;
    }

    public static void Log(string message)
    {
        Debug.Log("[" + instance.gameTime.TimeString + "] " + message);
    }

    public static void Log(string message, UnityEngine.Object obj)
    {
        Debug.Log("[" + instance.gameTime.TimeString + "] " + message, obj);
    }

    public static void LogError(string message)
    {
        Debug.LogError("[" + instance.gameTime.TimeString + "] " + message);
    }

    public static void LogError(string message, UnityEngine.Object obj)
    {
        Debug.LogError("[" + instance.gameTime.TimeString + "] " + message, obj);
    }

    public static void LogWarning(string message)
    {
        Debug.LogWarning("[" + instance.gameTime.TimeString + "] " + message);
    }
    public static void LogWarning(string message, UnityEngine.Object obj)
    {
        Debug.LogWarning("[" + instance.gameTime.TimeString + "] " + message, obj);
    }

    public static void LogSimulation(string message, string level = "sim")
    {
        Instance.simulationLogger.LogSimulation(message, level);
    }
}
