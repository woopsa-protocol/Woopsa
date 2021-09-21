using System;
using System.Collections.Generic;
using UnityEngine;
using Woopsa;

public class DemoClient : MonoBehaviour
{
    #region Unity Inspector

    [SerializeField]
    private string _serverUrl;

    #endregion

    #region Public Properties

    public WoopsaClient Client => _woopsaClient;

    #endregion

    #region Called by Unity Engine

    private void Awake()
    {
        try
        {
            // Creating Woopsa Client
            _woopsaClient = new WoopsaClient(_serverUrl);
            Debug.Log("Woopsa client created on URL: " + _serverUrl);
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    #endregion

    #region Private Members

    private WoopsaClient _woopsaClient;

    #endregion
}
