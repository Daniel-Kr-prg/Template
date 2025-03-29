using UnityEngine;

public class InputManager_KeysMapReceiver_Steam : FileReceiver_Steam<KeysMap>
{
    public InputManager_KeysMapReceiver_Steam() : base(ImportantFilepaths.KeysCloudPath)
    {
    }
}

public class InputManager_KeysMapReceiver_Local : FileReceiver_Local<KeysMap>
{
    public InputManager_KeysMapReceiver_Local(string path) : base(path)
    {
        if (path == null || path == "")
        {
            this.path = ImportantFilepaths.KeysConfigPath;
        }
    }
}
