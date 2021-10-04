using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Woopsa;

public class WoopsaSubscriptionTest : MonoBehaviour
{
    #region Unity Inspector

    [SerializeField]
    private GameObject _indicatorObject;

    #endregion

    #region Unity Engine

    private void Start()
    {
        _client = FindObjectOfType<DemoClient>()?.Client;

        _client.SubscriptionChannel.Subscribe("IsRunning", TestProperty_Updated);
    }

    private void Update()
    {
        // Not connected yet
        if (_lastIndicatorState == null)
            return;

        // Update indicator color to display indicator state in Unity player
        if (_lastIndicatorState.Value)
            _indicatorObject.GetComponent<Renderer>().material.color = Color.green;
        else
            _indicatorObject.GetComponent<Renderer>().material.color = Color.red;
    }

    #endregion

    #region Private Members

    private bool? _lastIndicatorState;
    WoopsaClient _client;

    private void TestProperty_Updated(object sender, WoopsaNotificationEventArgs e)
    {
        if (_lastIndicatorState == null)
            _lastIndicatorState = new bool();

        _lastIndicatorState = e.Notification.Value.ToBool();
    }

    #endregion
}
