using DanieloZ.Managers;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SaveManager : SingletonManager<SaveManager>
{
    private Save save;

    private FileReceiver<Save> receiver;

    private int _itemsCount;

    protected override void Awake()
    {
        base.Awake();

        save = new Save();
        receiver = new FileReceiver_Local<Save>(ImportantFilepaths.MainSavefilePath);
    }

    private void Start()
    {
        // Additional handling before stage changing

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_SaveManagerReady");
    }

    public static void IncrementSaveItemsCount()
    {
        Instance._itemsCount++;
    }

    public static void AddSaveItem(string key, string data, Action<float, string> onProgress = null)
    {
        Instance.save.items[key] = data;
        onProgress?.Invoke(Mathf.Lerp(0.3f, 0.7f, Instance.save.items.Count / Instance._itemsCount), "Saving");
    }

    public static void RemoveSaveItem(string key)
    {
        if (!Instance.save.items.ContainsKey(key))
        {
            Instance.DebugError($"no item with a key {key}");
            return;
        }
        Instance.save.items.Remove(key);
    }

    #region save-class handling
    /// <summary>
    /// Initiates the save process asynchronously from the interface layer.
    /// This method triggers the asynchronous save workflow using <see cref="SaveAsync"/>.
    /// </summary>
    /// <param name="onProgress">Callback to report progress, with a float representing the percentage and a string describing the current stage.</param>
    /// <param name="onSuccess">Callback invoked when the save process completes successfully.</param>
    /// <param name="onFail">Callback invoked if the save process encounters an error.</param>
    public static void Save(Action<float, string> onProgress = null, Action onSuccess = null, Action onFail = null)
    {
        Instance.StartCoroutine(Instance.SaveCoroutine(onProgress, onSuccess, onFail));
        //_ = SaveAsync(onProgress, onSuccess, onFail);
    }
    /// <summary>
    /// Coroutine-based save process for legacy systems or environments where coroutines are required.
    /// Handles clearing, saving items, and invoking events, similar to <see cref="SaveAsync"/>.
    /// </summary>
    /// <param name="onProgress">Callback to report progress as a float representing the percentage.</param>
    /// <param name="onSuccess">Callback invoked when the save process completes successfully.</param>
    /// <param name="onFail">Callback invoked if the save process encounters an error.</param>
    /// <returns>An enumerator used to execute the coroutine.</returns>
    IEnumerator SaveCoroutine(Action<float, string> onProgress, Action onSuccess, Action onFail)
    {
        DebugMessage("Starting save process...");
        try
        {
            save.items.Clear();
            _itemsCount = 0;
            EventManager.CallEvent(EventName.SaveManager_ItemsCount);
            EventManager.CallEvent(EventName.SaveManager_Save);
            receiver.SaveFile(save);
            DebugMessage("Save process complete.");
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            DebugError($"Save process failed: {ex.Message}");
            onFail?.Invoke();
        }

        yield return null;
    }
    /// <summary>
    /// Performs the asynchronous save process, handling all steps including clearing, counting, and saving items.
    /// Reports progress throughout the operation and invokes callbacks for success or failure.
    /// </summary>
    /// <param name="onProgress">Callback to report progress, with a float representing the percentage and a string describing the current stage.</param>
    /// <param name="onSuccess">Callback invoked when the save process completes successfully.</param>
    /// <param name="onFail">Callback invoked if the save process encounters an error.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public static async Task SaveAsync(Action<float, string> onProgress = null, Action onSuccess = null, Action onFail = null)
    {
        try
        {
            Instance.DebugMessage("Starting save process...");
            Instance.save.items.Clear();

            onProgress?.Invoke(0f, "Starting save process");

            await EventManager.CallEventAsync(EventName.SaveManager_ItemsCount);

            onProgress?.Invoke(0.1f, "Counting items to save");
            await Task.Delay(100);

            onProgress?.Invoke(0.3f, "Collecting items to save");

            await EventManager.CallEventAsync(EventName.SaveManager_Save, new object[] { onProgress });

            await Task.Delay(100);

            await Task.Run(() => Instance.receiver.SaveFile(Instance.save));
            onProgress?.Invoke(0.8f, "Saving the file");
            await Task.Delay(100);

            onProgress?.Invoke(0.95f, "Petting the cat");
            await Task.Delay(100);


            onProgress?.Invoke(1f, "Saved!");
            await Task.Delay(150);

            Instance.DebugMessage("Save process complete.");
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Instance.DebugError($"Save process failed: {ex.Message}");
            onFail?.Invoke();
        }
    }

    /// <summary>
    /// Initiates the load process asynchronously from the interface layer.
    /// This method triggers the asynchronous load workflow using <see cref="LoadAsync"/>.
    /// </summary>
    /// <param name="onProgress">Callback to report progress, with a float representing the percentage and a string describing the current stage.</param>
    /// <param name="onSuccess">Callback invoked when the load process completes successfully.</param>
    /// <param name="onFail">Callback invoked if the load process encounters an error.</param>
    public static void Load(Action<float, string> onProgress = null, Action onSuccess = null, Action onFail = null)
    {
        Instance.StartCoroutine(Instance.LoadCoroutine(onProgress, onSuccess, onFail));        
        //_ = LoadAsync(onProgress, onSuccess, onFail);
    }

    /// <summary>
    /// Coroutine-based load process for legacy systems or environments where coroutines are required.
    /// Handles loading files, applying data, and invoking events, similar to <see cref="LoadAsync"/>.
    /// </summary>
    /// <param name="onProgress">Callback to report progress as a float representing the percentage.</param>
    /// <param name="onSuccess">Callback invoked when the load process completes successfully.</param>
    /// <param name="onFail">Callback invoked if the load process encounters an error.</param>
    /// <returns>An enumerator used to execute the coroutine.</returns>
    IEnumerator LoadCoroutine(Action<float, string> onProgress, Action onSuccess, Action onFail)
    {
        DebugMessage("Starting load process...");
        try
        {
            save = receiver.LoadFile();

            if (save == null)
            {
                DebugWarning("Save file is empty or corrupted.");
                onFail?.Invoke();
                yield break;
            }

            EventManager.CallEvent(EventName.SaveManager_Load);

            DebugMessage("Load process complete.");
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            DebugError($"Load process failed: {ex.Message}");
            onFail?.Invoke();
        }

        yield return null;
    }
    /// <summary>
    /// Performs the asynchronous load process, handling all steps including file reading, applying data, and invoking events.
    /// Reports progress throughout the operation and invokes callbacks for success or failure.
    /// </summary>
    /// <param name="onProgress">Callback to report progress, with a float representing the percentage and a string describing the current stage.</param>
    /// <param name="onSuccess">Callback invoked when the load process completes successfully.</param>
    /// <param name="onFail">Callback invoked if the load process encounters an error.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    public static async Task LoadAsync(Action<float, string> onProgress = null, Action onSuccess = null, Action onFail = null)
    {
        try
        {
            Instance.DebugMessage("Starting load process...");

            onProgress?.Invoke(0f, "Initializing load process");
            await Task.Delay(100);

            var loadedSave = await Task.Run(() => Instance.receiver.LoadFile());

            if (loadedSave == null)
            {
                Instance.DebugWarning("Save file is empty or corrupted.");
                onFail?.Invoke();
                return;
            }

            Instance.save = loadedSave;

            onProgress?.Invoke(0.5f, "File loaded, applying data");
            await Task.Delay(200);

            await EventManager.CallEventAsync(EventName.SaveManager_Load);

            onProgress?.Invoke(0.9f, "Finalizing load process");
            await Task.Delay(200);

            Instance.DebugMessage("Load process complete.");
            onProgress?.Invoke(1f, "Load complete");
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Instance.DebugError($"Load process failed: {ex.Message}");
            onFail?.Invoke();
        }
    }
    #endregion

    #region Save-load SaveItem
    /// <summary>
    /// Loads and deserializes a saved item of type <typeparamref name="T"/> from the save data using the specified key.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the SaveItem to be loaded. Must derive from <see cref="SaveItem"/>.
    /// </typeparam>
    /// <param name="key">
    /// The unique identifier used to locate the saved data for the item.
    /// </param>
    /// <returns>
    /// An instance of type <typeparamref name="T"/> containing the deserialized data if the key exists in the save data; 
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// If the key does not exist in the save data, an error message is logged, and <c>null</c> is returned. 
    /// This method relies on the <see cref="IOManager.LoadStringJSON{T}"/> method for deserialization.
    /// </remarks>
    public static T Load<T>(string key)
    {
        if (Instance.save.items.TryGetValue(key, out var saveString))
        {
            return IOManager.LoadStringJSON<T>(saveString);
        }
        else
        {
            Instance.DebugError($"can't find the value with the key {key}");
            return default;
        }
    }

    #endregion
}

[System.Serializable]
public class Save
{
    public SerializedDictionary<string, string> items = new SerializedDictionary<string, string>();
}

/// <summary>
/// Base class for objects that need to be saved as part of the game's save system.
/// Provides functionality for saving and loading data associated with a specific item.
/// Each item is uniquely identified by an ID and can register itself with save-related events.
/// </summary>
[System.Serializable]
public abstract class SaveItem
{
    /// <summary>
    /// Unique identifier for the SaveItem. This ID is used to track and retrieve saved data for the item.
    /// </summary>
    public string id;

    /// <summary>
    /// The target object that the SaveItem represents. Typically holds the data or state to be saved or loaded.
    /// </summary>
    protected object target;

    protected SerializedDictionary<string, object> data = new SerializedDictionary<string, object>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveItem"/> class with the specified ID and target object.
    /// Registers the item with the save system for both counting and saving operations.
    /// </summary>
    /// <param name="id">The unique identifier for the SaveItem.</param>
    /// <param name="target">The object representing the data or state to be saved or loaded.</param>
    public SaveItem(string id, object target)
    {
        this.id = id;
        this.target = target;

        EventManager.AddCallback(EventName.SaveManager_ItemsCount, new Action<object[]>(x => SaveManager.IncrementSaveItemsCount()));
        EventManager.AddCallback(EventName.SaveManager_Save, new Action<object[]>((x) => { SaveManager.AddSaveItem(id, CreateSaveData(), (x == null ? null : x[0] as Action<float, string>));  }));
        EventManager.AddCallback(EventName.SaveManager_Load, new Action<object[]>((x) => { LoadCallback(); }));
    }

    protected abstract void LoadCallback();

    /// <summary>
    /// Loads the saved data for this item using its unique identifier.
    /// Attempts to retrieve and apply the saved data, invoking callbacks based on the outcome.
    /// </summary>
    /// <typeparam name="T">The type of the SaveItem being loaded.</typeparam>
    /// <param name="onLoadSuccess">
    /// A callback invoked when the item is successfully loaded. The loaded object is passed as an argument.
    /// </param>
    /// <param name="onLoadFailed">
    /// A callback invoked when the item fails to load. This can occur if no saved data exists for the item's ID.
    /// </param>
    public virtual void Load<T>(Action<T> onLoadSuccess = null, Action onLoadFailed = null)
    {
        T result = SaveManager.Load<T>(id);
        if (result != null)
        {
            onLoadSuccess?.Invoke(result);
        }
        else
        {
            onLoadFailed?.Invoke();
        }
    }

    /// <summary>
    /// Creates and returns the serialized save data for the target object of this SaveItem.
    /// This method delegates to the abstract <see cref="CreateSaveData(object)"/> implementation.
    /// </summary>
    /// <returns>A string containing the serialized save data for the target object.</returns>
    public string CreateSaveData() { return CreateSaveData(target); }

    /// <summary>
    /// Abstract method for generating serialized save data for a given object.
    /// Subclasses must implement this method to define how their specific data is serialized.
    /// </summary>
    /// <param name="sourceObject">The object whose data will be serialized for saving.</param>
    /// <returns>A string containing the serialized save data for the provided object.</returns>
    public abstract string CreateSaveData(object sourceObject);
}